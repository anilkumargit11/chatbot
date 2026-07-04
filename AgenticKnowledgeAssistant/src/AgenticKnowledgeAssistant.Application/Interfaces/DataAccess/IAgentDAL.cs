using AgenticKnowledgeAssistant.DTO.Models;

namespace AgenticKnowledgeAssistant.DAL.Interfaces;

public interface IAgentDAL
{
    Task<int> SaveEmbeddingDB(EmbeddingModel embeddingModel, CancellationToken cancellationToken = default);
    Task<IEnumerable<EmbeddingModel>> GetEmbeddingsDB(CancellationToken cancellationToken = default);
}
