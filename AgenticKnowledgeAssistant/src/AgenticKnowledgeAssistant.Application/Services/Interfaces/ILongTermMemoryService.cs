using AgenticKnowledgeAssistant.DTO.Models;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;

namespace AgenticKnowledgeAssistant.BAL.Interfaces;

public interface ILongTermMemoryService
{
    Task<UserMemoryModel> SaveMemoryAsync(int userId, SaveMemoryRequestDTO request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserMemoryModel>> SearchMemoriesAsync(int userId, MemorySearchRequestDTO request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MemoryCategoryModel>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<UserMemoryModel?> GetMemoryAsync(long memoryId, int userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateMemoryAsync(long memoryId, int userId, UpdateMemoryRequestDTO request, CancellationToken cancellationToken = default);
    Task<bool> DeleteMemoryAsync(long memoryId, int userId, CancellationToken cancellationToken = default);
    Task<string> BuildMemoryContextAsync(int userId, CancellationToken cancellationToken = default);
    Task<UserMemoryModel?> TrySaveExplicitMemoryAsync(int userId, string userMessage, CancellationToken cancellationToken = default);
}
