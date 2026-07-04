using AgenticKnowledgeAssistant.DTO.Models;

namespace AgenticKnowledgeAssistant.DAL.Interfaces;

public interface IUserAdminDAL
{
    Task<IEnumerable<AdminUserModel>> GetUsersDB(string? search, int? roleId, bool? isActive, CancellationToken cancellationToken = default);
    Task<AdminUserModel?> GetUserByIdDB(int id, CancellationToken cancellationToken = default);
    Task<int> CreateUserDB(AdminUserModel user, string passwordHash, int createdBy, CancellationToken cancellationToken = default);
    Task<int> UpdateUserDB(int id, AdminUserModel user, int modifiedBy, CancellationToken cancellationToken = default);
    Task<int> DeleteUserDB(int id, int modifiedBy, CancellationToken cancellationToken = default);
    Task<int> SetUserStatusDB(int id, bool isActive, int modifiedBy, CancellationToken cancellationToken = default);
    Task<int> ResetPasswordDB(int id, string passwordHash, int modifiedBy, CancellationToken cancellationToken = default);
}
