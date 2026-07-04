using AgenticKnowledgeAssistant.DTO.Models;

namespace AgenticKnowledgeAssistant.DAL.Interfaces;

public interface IAuthDAL
{
    Task<RegisterUserResultModel?> RegisterUserDB(string fullName, string email, string mobileNumber, string passwordHash, CancellationToken cancellationToken = default);
    Task<AuthUserModel?> LoginUserDB(string email, CancellationToken cancellationToken = default);
    Task<AuthUserModel?> ValidateUserDB(int userId, CancellationToken cancellationToken = default);
    Task<long> SaveRefreshTokenDB(int userId, string tokenHash, DateTime expiresAtUtc, CancellationToken cancellationToken = default);
    Task<RefreshTokenModel?> GetRefreshTokenDB(string tokenHash, CancellationToken cancellationToken = default);
    Task<int> RevokeRefreshTokenDB(string tokenHash, string? replacedByTokenHash, int? modifiedBy, CancellationToken cancellationToken = default);
    Task<int> UpdateLastLoginDB(int userId, CancellationToken cancellationToken = default);
    Task<long> LogLoginHistoryDB(int? userId, string email, string? ipAddress, string? userAgent, bool isSuccess, string? failureReason, CancellationToken cancellationToken = default);
    Task<int> GetRegisteredUserCountDB(CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSummaryModel>> GetUsersDB(bool? isActive = null, bool registeredTodayOnly = false, CancellationToken cancellationToken = default);
    Task<MfaSettingsModel?> GetMfaSettingsDB(int userId, CancellationToken cancellationToken = default);
    Task SaveMfaSettingsDB(int userId, bool emailOtpEnabled, bool smsOtpEnabled, string? authenticatorSecret, bool isMfaConfigured, string? backupCodes, CancellationToken cancellationToken = default);
}
