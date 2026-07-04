using AgenticKnowledgeAssistant.DTO.Models;

namespace AgenticKnowledgeAssistant.BAL.Interfaces;

public interface IImageContextService
{
    Task<ImageContextModel> AddOrUpdateAsync(Guid sessionGuid, string fileName, string contentType, string ocrText, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImageContextModel>> GetSessionImagesAsync(Guid sessionGuid, CancellationToken cancellationToken = default);
    Task ClearSessionAsync(Guid sessionGuid, CancellationToken cancellationToken = default);
}
