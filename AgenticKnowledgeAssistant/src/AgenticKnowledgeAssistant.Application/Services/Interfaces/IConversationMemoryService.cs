using AgenticKnowledgeAssistant.DTO.Models;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;

namespace AgenticKnowledgeAssistant.BAL.Interfaces;

public interface IConversationMemoryService
{
    Task<ConversationSessionModel> GetOrCreateSessionAsync(ChatRequestDTO request, int userId, CancellationToken cancellationToken = default);
    Task<string> BuildContextAsync(Guid sessionGuid, int userId, ChatRequestDTO request, CancellationToken cancellationToken = default);
    Task SaveExchangeAsync(Guid sessionGuid, int userId, ChatRequestDTO request, string answer, object? metadata, CancellationToken cancellationToken = default);
    Task<ConversationSessionModel> CreateSessionAsync(int userId, CreateChatSessionRequestDTO request, CancellationToken cancellationToken = default);
    Task<ConversationSessionModel?> GetSessionAsync(Guid sessionGuid, int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversationSessionModel>> SearchSessionsAsync(int userId, ConversationSearchRequestDTO request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversationMessageModel>> GetMessagesAsync(Guid sessionGuid, int userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<ConversationMessageModel> SaveMessageAsync(int userId, SaveChatMessageRequestDTO request, CancellationToken cancellationToken = default);
    Task<bool> UpdateSessionAsync(Guid sessionGuid, int userId, UpdateChatSessionRequestDTO request, CancellationToken cancellationToken = default);
    Task<bool> DeleteSessionAsync(Guid sessionGuid, int userId, CancellationToken cancellationToken = default);
}
