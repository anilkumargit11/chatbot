using AgenticKnowledgeAssistant.DTO.Models;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;

namespace AgenticKnowledgeAssistant.DAL.Interfaces;

public interface IConversationRepository
{
    Task<ConversationSessionModel> CreateSessionAsync(int userId, string title, CancellationToken cancellationToken = default);
    Task<ConversationSessionModel?> GetSessionAsync(Guid sessionGuid, int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversationSessionModel>> SearchSessionsAsync(int userId, ConversationSearchRequestDTO request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversationMessageModel>> GetMessagesAsync(Guid sessionGuid, int userId, int skip, int take, CancellationToken cancellationToken = default);
    Task<ConversationMessageModel> SaveMessageAsync(Guid sessionGuid, int userId, string role, string message, int? tokens, string? metadata, CancellationToken cancellationToken = default);
    Task<bool> UpdateSessionAsync(Guid sessionGuid, int userId, UpdateChatSessionRequestDTO request, CancellationToken cancellationToken = default);
    Task<bool> DeleteSessionAsync(Guid sessionGuid, int userId, CancellationToken cancellationToken = default);
}
