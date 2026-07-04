using AgenticKnowledgeAssistant.DTO.Models;

namespace AgenticKnowledgeAssistant.DAL.Interfaces;

public interface IRoleAdminDAL
{
    Task<IEnumerable<AdminRoleModel>> GetRolesDB(string? search, bool? isActive, CancellationToken cancellationToken = default);
    Task<AdminRoleModel?> GetRoleByIdDB(int id, CancellationToken cancellationToken = default);
    Task<int> CreateRoleDB(AdminRoleModel role, int createdBy, CancellationToken cancellationToken = default);
    Task<int> UpdateRoleDB(int id, AdminRoleModel role, int modifiedBy, CancellationToken cancellationToken = default);
    Task<int> DeleteRoleDB(int id, int modifiedBy, CancellationToken cancellationToken = default);
    Task<IEnumerable<PermissionModel>> GetPermissionsDB(int? roleId, CancellationToken cancellationToken = default);
    Task<int> AssignPermissionsDB(int roleId, IEnumerable<int> permissionIds, int modifiedBy, CancellationToken cancellationToken = default);
}
