using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using Microsoft.AspNetCore.Http;

namespace AgenticKnowledgeAssistant.BAL.Interfaces;

public interface IDocumentBAL
{
    Task<Response<object>> UploadDocument(IFormFile? file, CancellationToken cancellationToken = default);
    Task<Response<object>> GetDocuments(CancellationToken cancellationToken = default);
    Task<Response<object>> SearchDocuments(string query, CancellationToken cancellationToken = default);
    Task<Response<object>> DeleteDocument(int id, CancellationToken cancellationToken = default);
}
