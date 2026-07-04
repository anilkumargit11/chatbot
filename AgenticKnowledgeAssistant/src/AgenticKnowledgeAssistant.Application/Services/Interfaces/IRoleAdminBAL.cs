using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;

namespace AgenticKnowledgeAssistant.BAL.Interfaces;

public interface IRoleAdminBAL
{
    Task<Response<object>> GetRoles(string? search, bool? isActive, CancellationToken cancellationToken = default);
    Task<Response<object>> GetRoleById(int id, CancellationToken cancellationToken = default);
    Task<Response<object>> CreateRole(SaveRoleRequestDTO request, int currentUserId, CancellationToken cancellationToken = default);
    Task<Response<object>> UpdateRole(int id, SaveRoleRequestDTO request, int currentUserId, CancellationToken cancellationToken = default);
    Task<Response<object>> DeleteRole(int id, int currentUserId, CancellationToken cancellationToken = default);
    Task<Response<object>> GetPermissions(int? roleId, CancellationToken cancellationToken = default);
    Task<Response<object>> AssignPermissions(int roleId, AssignPermissionsRequestDTO request, int currentUserId, CancellationToken cancellationToken = default);
}
