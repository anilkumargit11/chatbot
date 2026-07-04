using System.Data;
using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.Models;

namespace AgenticKnowledgeAssistant.DAL;

public sealed class AuthDAL(ICommonDAL commonDAL) : IAuthDAL
{
    public Task<RegisterUserResultModel?> RegisterUserDB(string fullName, string email, string mobileNumber, string passwordHash, CancellationToken cancellationToken = default)
    {
        return commonDAL.QuerySingleOrDefaultAsync<RegisterUserResultModel>("dbo.usp_AI_RegisterUser", new[]
        {
            new SqlParameterDefinition { Name = "FullName", DbType = SqlDbType.NVarChar, Value = fullName },
            new SqlParameterDefinition { Name = "Email", DbType = SqlDbType.NVarChar, Value = email },
            new SqlParameterDefinition { Name = "MobileNumber", DbType = SqlDbType.NVarChar, Value = mobileNumber },
            new SqlParameterDefinition { Name = "PasswordHash", DbType = SqlDbType.NVarChar, Value = passwordHash }
        }, cancellationToken);
    }

    public Task<AuthUserModel?> LoginUserDB(string email, CancellationToken cancellationToken = default)
    {
        return commonDAL.QuerySingleOrDefaultAsync<AuthUserModel>("dbo.usp_AI_LoginUser", new[]
        {
            new SqlParameterDefinition { Name = "Email", DbType = SqlDbType.NVarChar, Value = email }
        }, cancellationToken);
    }

    public Task<AuthUserModel?> ValidateUserDB(int userId, CancellationToken cancellationToken = default)
    {
        return commonDAL.QuerySingleOrDefaultAsync<AuthUserModel>("dbo.usp_AI_ValidateUser", new[]
        {
            new SqlParameterDefinition { Name = "UserId", DbType = SqlDbType.Int, Value = userId }
        }, cancellationToken);
    }

    public Task<long> SaveRefreshTokenDB(int userId, string tokenHash, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
    {
        return commonDAL.ExecuteScalarAsync<long>("dbo.usp_AI_SaveRefreshToken", new[]
        {
            new SqlParameterDefinition { Name = "UserId", DbType = SqlDbType.Int, Value = userId },
            new SqlParameterDefinition { Name = "TokenHash", DbType = SqlDbType.NVarChar, Value = tokenHash },
            new SqlParameterDefinition { Name = "ExpiresAtUtc", DbType = SqlDbType.DateTime2, Value = expiresAtUtc },
            new SqlParameterDefinition { Name = "CreatedBy", DbType = SqlDbType.Int, Value = userId }
        }, cancellationToken)!;
    }

    public Task<RefreshTokenModel?> GetRefreshTokenDB(string tokenHash, CancellationToken cancellationToken = default)
    {
        return commonDAL.QuerySingleOrDefaultAsync<RefreshTokenModel>("dbo.usp_AI_GetRefreshToken", new[]
        {
            new SqlParameterDefinition { Name = "TokenHash", DbType = SqlDbType.NVarChar, Value = tokenHash }
        }, cancellationToken);
    }

    public Task<int> RevokeRefreshTokenDB(string tokenHash, string? replacedByTokenHash, int? modifiedBy, CancellationToken cancellationToken = default)
    {
        return commonDAL.ExecuteScalarAsync<int>("dbo.usp_AI_RevokeRefreshToken", new[]
        {
            new SqlParameterDefinition { Name = "TokenHash", DbType = SqlDbType.NVarChar, Value = tokenHash },
            new SqlParameterDefinition { Name = "ReplacedByTokenHash", DbType = SqlDbType.NVarChar, Value = replacedByTokenHash },
            new SqlParameterDefinition { Name = "ModifiedBy", DbType = SqlDbType.Int, Value = modifiedBy }
        }, cancellationToken)!;
    }

    public Task<int> UpdateLastLoginDB(int userId, CancellationToken cancellationToken = default)
    {
        return commonDAL.ExecuteScalarAsync<int>("dbo.usp_AI_UpdateLastLogin", new[]
        {
            new SqlParameterDefinition { Name = "UserId", DbType = SqlDbType.Int, Value = userId }
        }, cancellationToken)!;
    }

    public Task<long> LogLoginHistoryDB(int? userId, string email, string? ipAddress, string? userAgent, bool isSuccess, string? failureReason, CancellationToken cancellationToken = default)
    {
        return commonDAL.ExecuteScalarAsync<long>("dbo.usp_AI_LogLoginHistory", new[]
        {
            new SqlParameterDefinition { Name = "UserId", DbType = SqlDbType.Int, Value = userId },
            new SqlParameterDefinition { Name = "Email", DbType = SqlDbType.NVarChar, Value = email },
            new SqlParameterDefinition { Name = "IpAddress", DbType = SqlDbType.NVarChar, Value = ipAddress },
            new SqlParameterDefinition { Name = "UserAgent", DbType = SqlDbType.NVarChar, Value = userAgent },
            new SqlParameterDefinition { Name = "IsSuccess", DbType = SqlDbType.Bit, Value = isSuccess },
            new SqlParameterDefinition { Name = "FailureReason", DbType = SqlDbType.NVarChar, Value = failureReason }
        }, cancellationToken)!;
    }

    public async Task<int> GetRegisteredUserCountDB(CancellationToken cancellationToken = default)
    {
        return await commonDAL.ExecuteScalarAsync<int>("dbo.usp_AI_GetRegisteredUserCount", Array.Empty<SqlParameterDefinition>(), cancellationToken);
    }

    public async Task<IEnumerable<UserSummaryModel>> GetUsersDB(bool? isActive = null, bool registeredTodayOnly = false, CancellationToken cancellationToken = default)
    {
        return await commonDAL.QueryAsync<UserSummaryModel>("dbo.usp_AI_GetUsersForAssistant", new[]
        {
            new SqlParameterDefinition { Name = "IsActive", DbType = SqlDbType.Bit, Value = isActive },
            new SqlParameterDefinition { Name = "RegisteredTodayOnly", DbType = SqlDbType.Bit, Value = registeredTodayOnly }
        }, cancellationToken);
    }

    public Task<MfaSettingsModel?> GetMfaSettingsDB(int userId, CancellationToken cancellationToken = default)
    {
        return commonDAL.QuerySingleOrDefaultAsync<MfaSettingsModel>("dbo.usp_AI_GetMfaSettings", new[]
        {
            new SqlParameterDefinition { Name = "UserId", DbType = SqlDbType.Int, Value = userId }
        }, cancellationToken);
    }

    public Task SaveMfaSettingsDB(int userId, bool emailOtpEnabled, bool smsOtpEnabled, string? authenticatorSecret, bool isMfaConfigured, string? backupCodes, CancellationToken cancellationToken = default)
    {
        return commonDAL.ExecuteScalarAsync<int>("dbo.usp_AI_SaveMfaSettings", new[]
        {
            new SqlParameterDefinition { Name = "UserId", DbType = SqlDbType.Int, Value = userId },
            new SqlParameterDefinition { Name = "EmailOtpEnabled", DbType = SqlDbType.Bit, Value = emailOtpEnabled },
            new SqlParameterDefinition { Name = "SmsOtpEnabled", DbType = SqlDbType.Bit, Value = smsOtpEnabled },
            new SqlParameterDefinition { Name = "AuthenticatorSecret", DbType = SqlDbType.NVarChar, Value = authenticatorSecret },
            new SqlParameterDefinition { Name = "IsMfaConfigured", DbType = SqlDbType.Bit, Value = isMfaConfigured },
            new SqlParameterDefinition { Name = "BackupCodes", DbType = SqlDbType.NVarChar, Value = backupCodes }
        }, cancellationToken);
    }
}
