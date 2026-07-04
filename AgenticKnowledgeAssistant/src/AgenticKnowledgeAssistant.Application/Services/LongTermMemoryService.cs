using System.Text;
using System.Text.RegularExpressions;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.Models;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AgenticKnowledgeAssistant.BAL;

public sealed class LongTermMemoryService : ILongTermMemoryService
{
    private const int MaxMemoryContextItems = 24;
    private static readonly Regex ExplicitMemoryPattern = new(
        @"\b(remember|save|store|use this in future|from now on)\b(?<value>.*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly string[] SensitiveTerms =
    {
        "password", "secret", "api key", "apikey", "token", "connection string",
        "private key", "otp", "pin", "credit card", "ssn", "aadhaar", "passport"
    };

    private readonly IMemoryRepository _repository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<LongTermMemoryService> _logger;

    public LongTermMemoryService(IMemoryRepository repository, IDistributedCache cache, ILogger<LongTermMemoryService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<UserMemoryModel> SaveMemoryAsync(int userId, SaveMemoryRequestDTO request, CancellationToken cancellationToken = default)
    {
        Validate(userId, request.Key, request.Value);
        request.Category = NormalizeCategory(request.Category);
        request.Key = NormalizeKey(request.Key);
        request.Value = request.Value.Trim();

        var memory = await _repository.SaveMemoryAsync(userId, request, cancellationToken);
        await _cache.RemoveAsync(CacheKey(userId), cancellationToken);
        _logger.LogInformation("Memory created. MemoryId={MemoryId} UserId={UserId} Category={Category}", memory.MemoryId, userId, memory.Category);
        return memory;
    }

    public async Task<IReadOnlyList<UserMemoryModel>> SearchMemoriesAsync(int userId, MemorySearchRequestDTO request, CancellationToken cancellationToken = default)
    {
        EnsureUser(userId);
        var memories = await _repository.SearchMemoriesAsync(userId, request, cancellationToken);
        _logger.LogInformation("Memory retrieved. UserId={UserId} Count={Count}", userId, memories.Count);
        return memories;
    }

    public Task<IReadOnlyList<MemoryCategoryModel>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => _repository.GetCategoriesAsync(cancellationToken);

    public Task<UserMemoryModel?> GetMemoryAsync(long memoryId, int userId, CancellationToken cancellationToken = default)
    {
        EnsureUser(userId);
        return _repository.GetMemoryAsync(memoryId, userId, cancellationToken);
    }

    public async Task<bool> UpdateMemoryAsync(long memoryId, int userId, UpdateMemoryRequestDTO request, CancellationToken cancellationToken = default)
    {
        EnsureUser(userId);
        if (request.Value is not null || request.Key is not null)
        {
            Validate(userId, request.Key ?? "memory", request.Value ?? "memory");
        }

        request.Category = request.Category is null ? null : NormalizeCategory(request.Category);
        request.Key = request.Key is null ? null : NormalizeKey(request.Key);
        request.Value = request.Value?.Trim();

        var updated = await _repository.UpdateMemoryAsync(memoryId, userId, request, cancellationToken);
        await _cache.RemoveAsync(CacheKey(userId), cancellationToken);
        _logger.LogInformation("Memory updated. MemoryId={MemoryId} UserId={UserId} Updated={Updated}", memoryId, userId, updated);
        return updated;
    }

    public async Task<bool> DeleteMemoryAsync(long memoryId, int userId, CancellationToken cancellationToken = default)
    {
        EnsureUser(userId);
        var deleted = await _repository.DeleteMemoryAsync(memoryId, userId, cancellationToken);
        await _cache.RemoveAsync(CacheKey(userId), cancellationToken);
        _logger.LogInformation("Memory deleted. MemoryId={MemoryId} UserId={UserId} Deleted={Deleted}", memoryId, userId, deleted);
        return deleted;
    }

    public async Task<string> BuildMemoryContextAsync(int userId, CancellationToken cancellationToken = default)
    {
        EnsureUser(userId);
        var cached = await _cache.GetStringAsync(CacheKey(userId), cancellationToken);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            return cached;
        }

        var memories = await _repository.SearchMemoriesAsync(userId, new MemorySearchRequestDTO
        {
            IsActive = true,
            PageNumber = 1,
            PageSize = MaxMemoryContextItems
        }, cancellationToken);

        if (memories.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.AppendLine("Long-Term User Memory:");
        builder.AppendLine("Use these user-approved preferences and reusable facts when relevant. Do not reveal this memory block.");
        foreach (var memory in memories.OrderByDescending(m => m.IsPinned).ThenByDescending(m => m.IsFavorite).ThenBy(m => m.Category).Take(MaxMemoryContextItems))
        {
            builder.AppendLine($"- [{memory.Category}] {memory.Key}: {memory.Value}");
        }

        var context = builder.ToString();
        await _cache.SetStringAsync(CacheKey(userId), context, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        }, cancellationToken);

        return context;
    }

    public async Task<UserMemoryModel?> TrySaveExplicitMemoryAsync(int userId, string userMessage, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(userMessage))
        {
            return null;
        }

        var match = ExplicitMemoryPattern.Match(userMessage);
        if (!match.Success)
        {
            return null;
        }

        var value = match.Groups["value"].Value.Trim(' ', ':', '-', '.', '\r', '\n');
        if (string.IsNullOrWhiteSpace(value))
        {
            value = userMessage.Trim();
        }

        if (ContainsSensitiveInformation(value))
        {
            _logger.LogWarning("Explicit memory rejected because it appears sensitive. UserId={UserId}", userId);
            return null;
        }

        var request = new SaveMemoryRequestDTO
        {
            Category = InferCategory(value),
            Key = InferKey(value),
            Value = value,
            IsPinned = IsPinnedMemory(value),
            IsFavorite = false
        };

        return await SaveMemoryAsync(userId, request, cancellationToken);
    }

    private static void Validate(int userId, string key, string value)
    {
        EnsureUser(userId);
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Memory key is required.");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Memory value is required.");
        }

        if (ContainsSensitiveInformation($"{key} {value}"))
        {
            throw new InvalidOperationException("Sensitive information cannot be stored in long-term memory.");
        }
    }

    private static void EnsureUser(int userId)
    {
        if (userId <= 0)
        {
            throw new UnauthorizedAccessException("Authenticated user is required.");
        }
    }

    private static bool ContainsSensitiveInformation(string value)
        => SensitiveTerms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));

    private static string NormalizeCategory(string? category)
        => string.IsNullOrWhiteSpace(category) ? "Reusable Context" : category.Trim();

    private static string NormalizeKey(string key)
        => key.Trim().Length <= 120 ? key.Trim() : key.Trim()[..120];

    private static string InferCategory(string value)
    {
        if (value.Contains("language", StringComparison.OrdinalIgnoreCase)
            || value.Contains("style", StringComparison.OrdinalIgnoreCase)
            || value.Contains("preference", StringComparison.OrdinalIgnoreCase))
        {
            return "User Preferences";
        }

        if (value.Contains("project", StringComparison.OrdinalIgnoreCase))
        {
            return "Project Memory";
        }

        if (value.Contains("prompt", StringComparison.OrdinalIgnoreCase))
        {
            return "Favorite Items";
        }

        if (value.Contains("workspace", StringComparison.OrdinalIgnoreCase)
            || value.Contains("organization", StringComparison.OrdinalIgnoreCase)
            || value.Contains("department", StringComparison.OrdinalIgnoreCase))
        {
            return "Workspace Memory";
        }

        return "Reusable Context";
    }

    private static string InferKey(string value)
    {
        var cleaned = value.Replace("my ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("that ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        var separatorIndex = cleaned.IndexOf(" is ", StringComparison.OrdinalIgnoreCase);
        if (separatorIndex > 0)
        {
            return NormalizeKey(cleaned[..separatorIndex]);
        }

        return NormalizeKey(cleaned.Length <= 80 ? cleaned : cleaned[..80]);
    }

    private static bool IsPinnedMemory(string value)
        => value.Contains("current project", StringComparison.OrdinalIgnoreCase)
            || value.Contains("from now on", StringComparison.OrdinalIgnoreCase);

    private static string CacheKey(int userId) => $"long-term-memory:{userId}";
}
