using System.Data;
using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.Models;

namespace AgenticKnowledgeAssistant.DAL;

public sealed class UserAdminDAL(ICommonDAL commonDAL) : IUserAdminDAL
{
    public Task<IEnumerable<AdminUserModel>> GetUsersDB(string? search, int? roleId, bool? isActive, CancellationToken cancellationToken = default)
    {
        return commonDAL.QueryAsync<AdminUserModel>("dbo.usp_AI_Admin_GetUsers", new[]
        {
            new SqlParameterDefinition { Name = "Search", DbType = SqlDbType.NVarChar, Value = search },
            new SqlParameterDefinition { Name = "RoleId", DbType = SqlDbType.Int, Value = roleId },
            new SqlParameterDefinition { Name = "IsActive", DbType = SqlDbType.Bit, Value = isActive }
        }, cancellationToken);
    }

    public Task<AdminUserModel?> GetUserByIdDB(int id, CancellationToken cancellationToken = default)
    {
        return commonDAL.QuerySingleOrDefaultAsync<AdminUserModel>("dbo.usp_AI_Admin_GetUserById", new[]
        {
            new SqlParameterDefinition { Name = "Id", DbType = SqlDbType.Int, Value = id }
        }, cancellationToken);
    }

    public Task<int> CreateUserDB(AdminUserModel user, string passwordHash, int createdBy, CancellationToken cancellationToken = default)
    {
        return commonDAL.ExecuteScalarAsync<int>("dbo.usp_AI_Admin_CreateUser", new[]
        {
            new SqlParameterDefinition { Name = "UserName", DbType = SqlDbType.NVarChar, Value = user.UserName },
            new SqlParameterDefinition { Name = "FullName", DbType = SqlDbType.NVarChar, Value = user.FullName },
            new SqlParameterDefinition { Name = "Email", DbType = SqlDbType.NVarChar, Value = user.Email },
            new SqlParameterDefinition { Name = "MobileNumber", DbType = SqlDbType.NVarChar, Value = user.MobileNumber },
            new SqlParameterDefinition { Name = "PasswordHash", DbType = SqlDbType.NVarChar, Value = passwordHash },
            new SqlParameterDefinition { Name = "RoleId", DbType = SqlDbType.Int, Value = user.RoleId },
            new SqlParameterDefinition { Name = "IsActive", DbType = SqlDbType.Bit, Value = user.IsActive },
            new SqlParameterDefinition { Name = "CreatedBy", DbType = SqlDbType.Int, Value = createdBy }
        }, cancellationToken);
    }

    public Task<int> UpdateUserDB(int id, AdminUserModel user, int modifiedBy, CancellationToken cancellationToken = default)
    {
        return commonDAL.ExecuteScalarAsync<int>("dbo.usp_AI_Admin_UpdateUser", new[]
        {
            new SqlParameterDefinition { Name = "Id", DbType = SqlDbType.Int, Value = id },
            new SqlParameterDefinition { Name = "UserName", DbType = SqlDbType.NVarChar, Value = user.UserName },
            new SqlParameterDefinition { Name = "FullName", DbType = SqlDbType.NVarChar, Value = user.FullName },
            new SqlParameterDefinition { Name = "Email", DbType = SqlDbType.NVarChar, Value = user.Email },
            new SqlParameterDefinition { Name = "MobileNumber", DbType = SqlDbType.NVarChar, Value = user.MobileNumber },
            new SqlParameterDefinition { Name = "RoleId", DbType = SqlDbType.Int, Value = user.RoleId },
            new SqlParameterDefinition { Name = "IsActive", DbType = SqlDbType.Bit, Value = user.IsActive },
            new SqlParameterDefinition { Name = "ModifiedBy", DbType = SqlDbType.Int, Value = modifiedBy }
        }, cancellationToken);
    }

    public Task<int> DeleteUserDB(int id, int modifiedBy, CancellationToken cancellationToken = default)
    {
        return commonDAL.ExecuteScalarAsync<int>("dbo.usp_AI_Admin_DeleteUser", new[]
        {
            new SqlParameterDefinition { Name = "Id", DbType = SqlDbType.Int, Value = id },
            new SqlParameterDefinition { Name = "ModifiedBy", DbType = SqlDbType.Int, Value = modifiedBy }
        }, cancellationToken);
    }

    public Task<int> SetUserStatusDB(int id, bool isActive, int modifiedBy, CancellationToken cancellationToken = default)
    {
        return commonDAL.ExecuteScalarAsync<int>("dbo.usp_AI_Admin_SetUserStatus", new[]
        {
            new SqlParameterDefinition { Name = "Id", DbType = SqlDbType.Int, Value = id },
            new SqlParameterDefinition { Name = "IsActive", DbType = SqlDbType.Bit, Value = isActive },
            new SqlParameterDefinition { Name = "ModifiedBy", DbType = SqlDbType.Int, Value = modifiedBy }
        }, cancellationToken);
    }

    public Task<int> ResetPasswordDB(int id, string passwordHash, int modifiedBy, CancellationToken cancellationToken = default)
    {
        return commonDAL.ExecuteScalarAsync<int>("dbo.usp_AI_Admin_ResetUserPassword", new[]
        {
            new SqlParameterDefinition { Name = "Id", DbType = SqlDbType.Int, Value = id },
            new SqlParameterDefinition { Name = "PasswordHash", DbType = SqlDbType.NVarChar, Value = passwordHash },
            new SqlParameterDefinition { Name = "ModifiedBy", DbType = SqlDbType.Int, Value = modifiedBy }
        }, cancellationToken);
    }
}
