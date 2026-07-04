using AgenticKnowledgeAssistant.DTO.Models;

namespace AgenticKnowledgeAssistant.DAL.Interfaces;

public interface IChatDAL
{
    Task<int> SaveChatHistoryDB(ChatHistoryModel chatHistoryModel, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatHistoryModel>> GetRecentChatHistoryDB(int limit = 50, CancellationToken cancellationToken = default);
}
