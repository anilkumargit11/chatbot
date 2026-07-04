using System.Data;
using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.Models;

namespace AgenticKnowledgeAssistant.DAL;

public sealed class RoleAdminDAL(ICommonDAL commonDAL) : IRoleAdminDAL
{
    public Task<IEnumerable<AdminRoleModel>> GetRolesDB(string? search, bool? isActive, CancellationToken cancellationToken = default)
    {
        return commonDAL.QueryAsync<AdminRoleModel>("dbo.usp_AI_Admin_GetRoles", new[]
        {
            new SqlParameterDefinition { Name = "Search", DbType = SqlDbType.NVarChar, Value = search },
            new SqlParameterDefinition { Name = "IsActive", DbType = SqlDbType.Bit, Value = isActive }
        }, cancellationToken);
    }

    public Task<AdminRoleModel?> GetRoleByIdDB(int id, CancellationToken cancellationToken = default)
    {
        return commonDAL.QuerySingleOrDefaultAsync<AdminRoleModel>("dbo.usp_AI_Admin_GetRoleById", new[]
        {
            new SqlParameterDefinition { Name = "Id", DbType = SqlDbType.Int, Value = id }
        }, cancellationToken);
    }

    public Task<int> CreateRoleDB(AdminRoleModel role, int createdBy, CancellationToken cancellationToken = default)
    {
        return commonDAL.ExecuteScalarAsync<int>("dbo.usp_AI_Admin_CreateRole", new[]
        {
            new SqlParameterDefinition { Name = "RoleName", DbType = SqlDbType.NVarChar, Value = role.RoleName },
            new SqlParameterDefinition { Name = "Description", DbType = SqlDbType.NVarChar, Value = role.Description },
            new SqlParameterDefinition { Name = "IsActive", DbType = SqlDbType.Bit, Value = role.IsActive },
            new SqlParameterDefinition { Name = "CreatedBy", DbType = SqlDbType.Int, Value = createdBy }
        }, cancellationToken);
    }

    public Task<int> UpdateRoleDB(int id, AdminRoleModel role, int modifiedBy, CancellationToken cancellationToken = default)
    {
        return commonDAL.ExecuteScalarAsync<int>("dbo.usp_AI_Admin_UpdateRole", new[]
        {
            new SqlParameterDefinition { Name = "Id", DbType = SqlDbType.Int, Value = id },
            new SqlParameterDefinition { Name = "RoleName", DbType = SqlDbType.NVarChar, Value = role.RoleName },
            new SqlParameterDefinition { Name = "Description", DbType = SqlDbType.NVarChar, Value = role.Description },
            new SqlParameterDefinition { Name = "IsActive", DbType = SqlDbType.Bit, Value = role.IsActive },
            new SqlParameterDefinition { Name = "ModifiedBy", DbType = SqlDbType.Int, Value = modifiedBy }
        }, cancellationToken);
    }

    public Task<int> DeleteRoleDB(int id, int modifiedBy, CancellationToken cancellationToken = default)
    {
        return commonDAL.ExecuteScalarAsync<int>("dbo.usp_AI_Admin_DeleteRole", new[]
        {
            new SqlParameterDefinition { Name = "Id", DbType = SqlDbType.Int, Value = id },
            new SqlParameterDefinition { Name = "ModifiedBy", DbType = SqlDbType.Int, Value = modifiedBy }
        }, cancellationToken);
    }

    public Task<IEnumerable<PermissionModel>> GetPermissionsDB(int? roleId, CancellationToken cancellationToken = default)
    {
        return commonDAL.QueryAsync<PermissionModel>("dbo.usp_AI_Admin_GetPermissions", new[]
        {
            new SqlParameterDefinition { Name = "RoleId", DbType = SqlDbType.Int, Value = roleId }
        }, cancellationToken);
    }

    public Task<int> AssignPermissionsDB(int roleId, IEnumerable<int> permissionIds, int modifiedBy, CancellationToken cancellationToken = default)
    {
        return commonDAL.ExecuteScalarAsync<int>("dbo.usp_AI_Admin_AssignRolePermissions", new[]
        {
            new SqlParameterDefinition { Name = "RoleId", DbType = SqlDbType.Int, Value = roleId },
            new SqlParameterDefinition { Name = "PermissionIds", DbType = SqlDbType.NVarChar, Value = string.Join(',', permissionIds.Distinct()) },
            new SqlParameterDefinition { Name = "ModifiedBy", DbType = SqlDbType.Int, Value = modifiedBy }
        }, cancellationToken);
    }
}
