using AgenticKnowledgeAssistant.DTO.Models;

namespace AgenticKnowledgeAssistant.DAL.Interfaces;

public interface IDocumentDAL
{
    Task<int> SaveDocumentDB(DocumentModel documentModel, CancellationToken cancellationToken = default);
    Task<DocumentModel?> GetDocumentByIdDB(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<DocumentModel>> GetDocumentsByIdsDB(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    Task<IEnumerable<DocumentModel>> GetDocumentsDB(CancellationToken cancellationToken = default);
    Task<IEnumerable<DocumentModel>> SearchDocumentsDB(string query, CancellationToken cancellationToken = default);
    Task<bool> DeleteDocumentDB(int id, CancellationToken cancellationToken = default);
}
