using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;
using Microsoft.AspNetCore.Http;

namespace AgenticKnowledgeAssistant.BAL.Interfaces;

public interface IRagService
{
    Task<Response<object>> UploadAsync(IFormFile? file, int userId, CancellationToken cancellationToken = default);
    Task<Response<object>> IndexAsync(RagIndexRequestDTO request, int userId, CancellationToken cancellationToken = default);
    Task<Response<object>> SearchAsync(RagSearchRequestDTO request, int userId, CancellationToken cancellationToken = default);
    Task<Response<object>> ChatAsync(RagChatRequestDTO request, int userId, CancellationToken cancellationToken = default);
    Task<Response<object>> GetDocumentAsync(long id, int userId, CancellationToken cancellationToken = default);
    Task<Response<object>> DeleteDocumentAsync(long id, int userId, CancellationToken cancellationToken = default);
}
