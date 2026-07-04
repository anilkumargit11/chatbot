using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using AgenticKnowledgeAssistant.DTO.Models;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;
using AgenticKnowledgeAssistant.DTO.ResponseDTOs;
using Microsoft.Extensions.Logging;

namespace AgenticKnowledgeAssistant.BAL;

public sealed class ChatBAL : IChatBAL
{
    private readonly IAgentBAL _agentBAL;
    private readonly IChatDAL _chatDAL;
    private readonly ICommonBAL _commonBAL;
    private readonly IConversationMemoryService _conversationMemoryService;
    private readonly ILongTermMemoryService _longTermMemoryService;
    private readonly ILogger<ChatBAL> _logger;

    public ChatBAL(IAgentBAL agentBAL, IChatDAL chatDAL, ICommonBAL commonBAL, IConversationMemoryService conversationMemoryService, ILongTermMemoryService longTermMemoryService, ILogger<ChatBAL> logger)
    {
        _agentBAL = agentBAL;
        _chatDAL = chatDAL;
        _commonBAL = commonBAL;
        _conversationMemoryService = conversationMemoryService;
        _longTermMemoryService = longTermMemoryService;
        _logger = logger;
    }

    public async Task<Response<object>> Chat(ChatRequestDTO request, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        try
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Question))
            {
                return _commonBAL.Failure((int)CommonResponse.CommonResponseErrorCodes.InvalidRequest, "Question is required", startTime);
            }

            var originalQuestion = request.Question;
            request.OriginalQuestion = originalQuestion;
            var sessionGuid = request.SessionGuid;
            var userId = Math.Max(0, request.UserId ?? 0);

            if (userId > 0)
            {
                try
                {
                    await _longTermMemoryService.TrySaveExplicitMemoryAsync(userId, originalQuestion, cancellationToken);

                    var session = await _conversationMemoryService.GetOrCreateSessionAsync(request, userId, cancellationToken);
                    sessionGuid = session.SessionGuid;
                    var conversationContext = await _conversationMemoryService.BuildContextAsync(session.SessionGuid, userId, request, cancellationToken);
                    var longTermContext = await _longTermMemoryService.BuildMemoryContextAsync(userId, cancellationToken);
                    request.Question = $"{longTermContext}{Environment.NewLine}{conversationContext}{Environment.NewLine}Current User Message:{Environment.NewLine}{originalQuestion}";
                }
                catch (Exception memoryEx)
                {
                    request.Question = originalQuestion;
                    _logger.LogWarning(memoryEx, "Memory context could not be loaded. Chat will continue without memory.");
                }
            }

            var response = await _agentBAL.HandleAgentRequest(request, cancellationToken);
            request.Question = originalQuestion;

            if (response is ChatResponseDTO chatResponse)
            {
                chatResponse.SessionGuid = sessionGuid;
            }

            if (userId > 0 && sessionGuid.HasValue)
            {
                try
                {
                    await _conversationMemoryService.SaveExchangeAsync(sessionGuid.Value, userId, request, response.Answer, new
                    {
                        response.Sources,
                        response.ToolUsed,
                        response.ConfidenceScore,
                        response.ResponseTimeMs,
                        response.PromptTokens,
                        response.CompletionTokens,
                        response.TotalTokens,
                        response.DetectedLanguage
                    }, cancellationToken);
                }
                catch (Exception memoryEx)
                {
                    _logger.LogWarning(memoryEx, "Conversation memory messages could not be saved. Legacy chat history will still be saved.");
                }
            }

            await _chatDAL.SaveChatHistoryDB(new ChatHistoryModel
            {
                Question = originalQuestion,
                Response = response.Answer,
                CreatedDate = DateTime.UtcNow
            }, cancellationToken);

            return _commonBAL.Success(response, startTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChatBAL.Chat failed");
            return _commonBAL.Failure((int)CommonResponse.CommonResponseErrorCodes.TechnicalError, "Technical Error", startTime);
        }
    }
}
