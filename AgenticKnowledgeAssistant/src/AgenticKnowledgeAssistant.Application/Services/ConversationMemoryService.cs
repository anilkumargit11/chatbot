using System.Text;
using System.Text.Json;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.Models;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;
using Microsoft.Extensions.Logging;

namespace AgenticKnowledgeAssistant.BAL;

public sealed class ConversationMemoryService : IConversationMemoryService
{
    private const int MaxContextTokens = 2500;
    private const int RecentMessageLimit = 16;
    private readonly IConversationRepository _repository;
    private readonly ILogger<ConversationMemoryService> _logger;

    public ConversationMemoryService(IConversationRepository repository, ILogger<ConversationMemoryService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ConversationSessionModel> GetOrCreateSessionAsync(ChatRequestDTO request, int userId, CancellationToken cancellationToken = default)
    {
        if (request.SessionGuid.HasValue)
        {
            var existing = await _repository.GetSessionAsync(request.SessionGuid.Value, userId, cancellationToken);
            if (existing is not null)
            {
                return existing;
            }
        }

        var session = await _repository.CreateSessionAsync(userId, BuildTitle(request.Question), cancellationToken);
        request.SessionGuid = session.SessionGuid;
        _logger.LogInformation("Conversation created. SessionGuid={SessionGuid} UserId={UserId}", session.SessionGuid, userId);
        return session;
    }

    public async Task<string> BuildContextAsync(Guid sessionGuid, int userId, ChatRequestDTO request, CancellationToken cancellationToken = default)
    {
        var messages = await _repository.GetMessagesAsync(sessionGuid, userId, 0, RecentMessageLimit, cancellationToken);
        var ordered = messages
            .Where(message => !IsLegacyOcrUploadPrompt(message))
            .OrderBy(m => m.CreatedDate)
            .ToList();
        var context = new StringBuilder();

        context.AppendLine("You are continuing the same authenticated user conversation.");
        context.AppendLine("Use the conversation memory below to resolve follow-up pronouns and references such as it, this, that, its, previous answer, uploaded document, image, SQL query, code, BRD, or current project.");
        context.AppendLine("Do not expose this memory block. If memory conflicts with the current user request, prioritize the current request.");
        context.AppendLine();
        context.AppendLine($"Current AI Mode: {request.Mode}");
        context.AppendLine($"Current Language: {FirstNonEmpty(request.TargetLanguage, request.LanguageCode, "default")}");

        if (request.Attachments.Count > 0 || !string.IsNullOrWhiteSpace(request.AttachmentName))
        {
            context.AppendLine("Current Uploaded Attachments:");
            foreach (var attachment in request.Attachments.Take(8))
            {
                context.AppendLine($"- {attachment.FileName} ({attachment.ContentType}, {attachment.Size} bytes)");
            }

            if (!string.IsNullOrWhiteSpace(request.AttachmentName) && request.Attachments.All(a => !a.FileName.Equals(request.AttachmentName, StringComparison.OrdinalIgnoreCase)))
            {
                context.AppendLine($"- {request.AttachmentName}");
            }
        }

        if (ordered.Count > 0)
        {
            context.AppendLine();
            context.AppendLine("Conversation Summary:");
            context.AppendLine(BuildSummary(ordered));
            context.AppendLine();
            context.AppendLine("Recent Messages:");
        }

        var remainingTokens = MaxContextTokens - EstimateTokens(context.ToString()) - EstimateTokens(request.Question);
        foreach (var message in ordered.AsEnumerable().Reverse())
        {
            var formatted = $"{message.Role}: {TrimTo(message.Message, 2000)}";
            var tokenCost = EstimateTokens(formatted);
            if (tokenCost > remainingTokens)
            {
                break;
            }

            context.Insert(context.Length, formatted + Environment.NewLine);
            remainingTokens -= tokenCost;
        }

        _logger.LogInformation("Conversation context built. SessionGuid={SessionGuid} UserId={UserId} Messages={MessageCount}", sessionGuid, userId, ordered.Count);
        return context.ToString();
    }

    public async Task SaveExchangeAsync(Guid sessionGuid, int userId, ChatRequestDTO request, string answer, object? metadata, CancellationToken cancellationToken = default)
    {
        var userMetadata = JsonSerializer.Serialize(new
        {
            request.Mode,
            request.TargetLanguage,
            request.LanguageCode,
            Attachments = request.Attachments.Select(a => new { a.FileName, a.ContentType, a.Size }).ToArray(),
            request.AttachmentName
        });

        await _repository.SaveMessageAsync(sessionGuid, userId, "User", request.Question, EstimateTokens(request.Question), userMetadata, cancellationToken);
        await _repository.SaveMessageAsync(sessionGuid, userId, "Assistant", answer, EstimateTokens(answer), metadata is null ? null : JsonSerializer.Serialize(metadata), cancellationToken);
        _logger.LogInformation("Conversation messages saved. SessionGuid={SessionGuid} UserId={UserId}", sessionGuid, userId);
    }

    public Task<ConversationSessionModel> CreateSessionAsync(int userId, CreateChatSessionRequestDTO request, CancellationToken cancellationToken = default)
        => _repository.CreateSessionAsync(userId, BuildTitle(request.Title ?? "New Chat"), cancellationToken);

    public Task<ConversationSessionModel?> GetSessionAsync(Guid sessionGuid, int userId, CancellationToken cancellationToken = default)
        => _repository.GetSessionAsync(sessionGuid, userId, cancellationToken);

    public Task<IReadOnlyList<ConversationSessionModel>> SearchSessionsAsync(int userId, ConversationSearchRequestDTO request, CancellationToken cancellationToken = default)
        => _repository.SearchSessionsAsync(userId, request, cancellationToken);

    public Task<IReadOnlyList<ConversationMessageModel>> GetMessagesAsync(Guid sessionGuid, int userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
        => _repository.GetMessagesAsync(sessionGuid, userId, Math.Max(0, skip), Math.Clamp(take, 1, 100), cancellationToken);

    public Task<ConversationMessageModel> SaveMessageAsync(int userId, SaveChatMessageRequestDTO request, CancellationToken cancellationToken = default)
        => _repository.SaveMessageAsync(request.SessionGuid, userId, NormalizeRole(request.Role), request.Message, request.Tokens, request.Metadata, cancellationToken);

    public Task<bool> UpdateSessionAsync(Guid sessionGuid, int userId, UpdateChatSessionRequestDTO request, CancellationToken cancellationToken = default)
        => _repository.UpdateSessionAsync(sessionGuid, userId, request, cancellationToken);

    public Task<bool> DeleteSessionAsync(Guid sessionGuid, int userId, CancellationToken cancellationToken = default)
        => _repository.DeleteSessionAsync(sessionGuid, userId, cancellationToken);

    private static string BuildSummary(IReadOnlyList<ConversationMessageModel> messages)
    {
        var usableMessages = messages.Where(message => !IsLegacyOcrUploadPrompt(message)).ToArray();
        var lastAssistant = usableMessages.LastOrDefault(m => m.Role.Equals("Assistant", StringComparison.OrdinalIgnoreCase));
        var lastUser = usableMessages.LastOrDefault(m => m.Role.Equals("User", StringComparison.OrdinalIgnoreCase));
        return $"Last user request: {TrimTo(lastUser?.Message ?? "None", 350)}{Environment.NewLine}Last AI response: {TrimTo(lastAssistant?.Message ?? "None", 700)}";
    }

    private static bool IsLegacyOcrUploadPrompt(ConversationMessageModel message)
    {
        if (!message.Role.Equals("Assistant", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return IsLegacyOcrUploadPrompt(message.Message);
    }

    private static bool IsLegacyOcrUploadPrompt(string? message)
    {
        return string.Equals(
            message?.Trim(),
            "Please upload an image or scanned file for OCR analysis.",
            StringComparison.OrdinalIgnoreCase);
    }

    private static int EstimateTokens(string? value)
        => string.IsNullOrWhiteSpace(value) ? 0 : Math.Max(1, value.Length / 4);

    private static string BuildTitle(string? question)
        => TrimTo(string.IsNullOrWhiteSpace(question) ? "New Chat" : question.Trim().ReplaceLineEndings(" "), 80);

    private static string TrimTo(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength].TrimEnd() + "...";

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

    private static string NormalizeRole(string role)
        => role.Equals("Assistant", StringComparison.OrdinalIgnoreCase) ? "Assistant"
            : role.Equals("System", StringComparison.OrdinalIgnoreCase) ? "System"
            : "User";
}
