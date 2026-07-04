using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.Common.Extensions;
using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using AgenticKnowledgeAssistant.DTO.Models;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;

namespace AgenticKnowledgeAssistant.BAL;

public sealed class RoleAdminBAL(IRoleAdminDAL roleAdminDAL, ICommonBAL commonBAL) : IRoleAdminBAL
{
    public async Task<Response<object>> GetRoles(string? search, bool? isActive, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var roles = await roleAdminDAL.GetRolesDB(search, isActive, cancellationToken);
        return commonBAL.Success(roles, startTime);
    }

    public async Task<Response<object>> GetRoleById(int id, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var role = await roleAdminDAL.GetRoleByIdDB(id, cancellationToken);
        return role is null
            ? commonBAL.Failure(404, "Role not found", startTime)
            : commonBAL.Success(role, startTime);
    }

    public async Task<Response<object>> CreateRole(SaveRoleRequestDTO request, int currentUserId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var validation = ValidateRole(request);
        if (!string.IsNullOrWhiteSpace(validation))
        {
            return commonBAL.Failure(400, validation, startTime);
        }

        var roleId = await roleAdminDAL.CreateRoleDB(ToModel(request), currentUserId, cancellationToken);
        return roleId <= 0
            ? commonBAL.Failure(409, "Role already exists", startTime)
            : commonBAL.Success(new { id = roleId }, startTime);
    }

    public async Task<Response<object>> UpdateRole(int id, SaveRoleRequestDTO request, int currentUserId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var validation = ValidateRole(request);
        if (!string.IsNullOrWhiteSpace(validation))
        {
            return commonBAL.Failure(400, validation, startTime);
        }

        var rows = await roleAdminDAL.UpdateRoleDB(id, ToModel(request), currentUserId, cancellationToken);
        return rows <= 0
            ? commonBAL.Failure(404, "Role not found or duplicate role name", startTime)
            : commonBAL.Success(new { id }, startTime);
    }

    public async Task<Response<object>> DeleteRole(int id, int currentUserId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var rows = await roleAdminDAL.DeleteRoleDB(id, currentUserId, cancellationToken);
        return rows <= 0
            ? commonBAL.Failure(409, "Role not found, is a system role, or is assigned to active users", startTime)
            : commonBAL.Success(new { id }, startTime);
    }

    public async Task<Response<object>> GetPermissions(int? roleId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var permissions = await roleAdminDAL.GetPermissionsDB(roleId, cancellationToken);
        return commonBAL.Success(permissions, startTime);
    }

    public async Task<Response<object>> AssignPermissions(int roleId, AssignPermissionsRequestDTO request, int currentUserId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var rows = await roleAdminDAL.AssignPermissionsDB(roleId, request.PermissionIds, currentUserId, cancellationToken);
        return rows <= 0
            ? commonBAL.Failure(404, "Role not found", startTime)
            : commonBAL.Success(new { roleId, totalPermissions = request.PermissionIds.Count }, startTime);
    }

    private static AdminRoleModel ToModel(SaveRoleRequestDTO request)
    {
        return new AdminRoleModel
        {
            RoleName = request.RoleName.Trim(),
            Description = request.Description.Trim(),
            IsActive = request.IsActive
        };
    }

    private static string ValidateRole(SaveRoleRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.RoleName)) return "Role name is required";
        if (request.RoleName.Length > 100) return "Role name cannot exceed 100 characters";
        return string.Empty;
    }
}
