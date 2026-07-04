using AgenticKnowledgeAssistant.DTO.Models;

namespace AgenticKnowledgeAssistant.DAL.Interfaces;

public interface IRagRepository
{
    Task<long> CreateDocumentAsync(RagDocumentModel document, CancellationToken cancellationToken = default);
    Task UpdateDocumentStatusAsync(long documentId, int userId, string status, int chunkCount, int embeddingCount, string? summary, string? metadata, CancellationToken cancellationToken = default);
    Task<RagDocumentModel?> GetDocumentAsync(long documentId, int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RagDocumentModel>> GetDocumentsAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteDocumentAsync(long documentId, int userId, CancellationToken cancellationToken = default);
    Task DeleteChunksAsync(long documentId, int userId, CancellationToken cancellationToken = default);
    Task<long> SaveChunkAsync(long documentId, int userId, RagChunkModel chunk, string? embeddingJson, string provider, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RagSearchResultModel>> KeywordSearchAsync(int userId, string query, IReadOnlyList<long> documentIds, int topK, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(RagSearchResultModel Chunk, string VectorData)>> GetSearchableEmbeddingsAsync(int userId, IReadOnlyList<long> documentIds, CancellationToken cancellationToken = default);
    Task SaveSearchHistoryAsync(int userId, string query, int resultCount, string searchType, CancellationToken cancellationToken = default);
}
