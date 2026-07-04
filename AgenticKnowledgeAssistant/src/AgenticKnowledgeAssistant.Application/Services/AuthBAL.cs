using System.Security.Cryptography;
using System.Text;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.Common.JWT;
using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using AgenticKnowledgeAssistant.DTO.Models;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;
using AgenticKnowledgeAssistant.DTO.ResponseDTOs;
using AgenticKnowledgeAssistant.Security.Encryption;
using Microsoft.Extensions.Options;

namespace AgenticKnowledgeAssistant.BAL;

public sealed class AuthBAL(
    IAuthDAL authDAL,
    IJwtTokenService jwtTokenService,
    IOptions<JwtOptions> jwtOptions,
    AgenticKnowledgeAssistant.Security.Authentication.IMfaService mfaService) : IAuthBAL
{
    public async Task<Response<object>> Register(RegisterRequestDTO request, CancellationToken cancellationToken = default)
    {
        var validationMessage = ValidateRegister(request);
        if (!string.IsNullOrEmpty(validationMessage))
        {
            return Failure(CommonResponse.CommonResponseErrorCodes.InvalidRequest, validationMessage);
        }

        var passwordHash = PasswordHasher.Hash(request.Password);
        var result = await authDAL.RegisterUserDB(request.FullName.Trim(), request.Email.Trim().ToLowerInvariant(), request.MobileNumber.Trim(), passwordHash, cancellationToken);
        if (result is null)
        {
            return Failure(CommonResponse.CommonResponseErrorCodes.FailedToSave, "Unable to register user");
        }

        if (result.IsDuplicateEmail)
        {
            return Failure(CommonResponse.CommonResponseErrorCodes.InvalidRequest, "Email already exists");
        }

        return Success(new { UserId = result.Id }, "Registration successful");
    }

    public async Task<Response<object>> Login(LoginRequestDTO request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var email = (string.IsNullOrWhiteSpace(request.Email) ? request.UserName : request.Email).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Failure(CommonResponse.CommonResponseErrorCodes.InvalidRequest, "Email and password are required");
        }

        var user = await authDAL.LoginUserDB(email, cancellationToken);
        if (user is null || !user.IsActive || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            await authDAL.LogLoginHistoryDB(user?.Id, email, ipAddress, userAgent, false, "Invalid credentials", cancellationToken);
            return Failure(CommonResponse.CommonResponseErrorCodes.Unauthorized, "Invalid email or password");
        }

        // Check if MFA is enabled/configured for this user
        var mfaSettings = await authDAL.GetMfaSettingsDB(user.Id, cancellationToken);
        if (mfaSettings is not null && mfaSettings.IsMfaConfigured)
        {
            // Return intermediate response with MfaToken
            var mfaToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Id}:{Guid.NewGuid():N}"));
            return Success(new LoginResponseDTO
            {
                IsMfaRequired = true,
                MfaToken = mfaToken,
                User = ToUserDetails(user)
            }, "MFA verification required");
        }

        var validUser = await GetAuthorizationUser(user, cancellationToken);
        var roles = GetRoles(validUser);
        var permissions = GetPermissions(validUser);
        var token = jwtTokenService.GenerateToken(user.Id, user.Email, roles, permissions);
        var refreshToken = GenerateRefreshToken();
        var refreshExpires = DateTime.UtcNow.AddDays(request.RememberMe ? jwtOptions.Value.RememberMeRefreshTokenExpiryDays : jwtOptions.Value.RefreshTokenExpiryDays);
        var refreshTokenHash = HashToken(refreshToken);

        await authDAL.SaveRefreshTokenDB(user.Id, refreshTokenHash, refreshExpires, cancellationToken);
        await authDAL.UpdateLastLoginDB(user.Id, cancellationToken);
        await authDAL.LogLoginHistoryDB(user.Id, email, ipAddress, userAgent, true, null, cancellationToken);

        token.RefreshToken = refreshToken;
        token.RefreshTokenExpiresAtUtc = refreshExpires;
        token.User = ToUserDetails(user);
        token.Roles = roles;
        token.Permissions = permissions;

        return Success(token, "Login successful");
    }

    public async Task<Response<object>> VerifyMfaLogin(string mfaToken, string code, bool rememberMe, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(mfaToken) || string.IsNullOrWhiteSpace(code))
            {
                return Failure(CommonResponse.CommonResponseErrorCodes.InvalidRequest, "MFA token and code are required");
            }

            var decryptedBytes = Convert.FromBase64String(mfaToken);
            var decryptedString = Encoding.UTF8.GetString(decryptedBytes);
            var parts = decryptedString.Split(':');
            if (parts.Length < 1 || !int.TryParse(parts[0], out var userId))
            {
                return Failure(CommonResponse.CommonResponseErrorCodes.InvalidRequest, "Invalid MFA token");
            }

            var mfaSettings = await authDAL.GetMfaSettingsDB(userId, cancellationToken);
            if (mfaSettings is null || !mfaSettings.IsMfaConfigured)
            {
                return Failure(CommonResponse.CommonResponseErrorCodes.InvalidRequest, "MFA is not configured for this user");
            }

            // Verify TOTP or backup codes
            var isTotpValid = mfaService.VerifyTotp(mfaSettings.AuthenticatorSecret ?? string.Empty, code);
            var isBackupValid = false;

            if (!isTotpValid && !string.IsNullOrWhiteSpace(mfaSettings.BackupCodes))
            {
                var backupList = mfaSettings.BackupCodes.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (backupList.Contains(code))
                {
                    isBackupValid = true;
                    // Remove code
                    var newBackupCodes = string.Join(",", backupList.Where(c => c != code));
                    await authDAL.SaveMfaSettingsDB(
                        userId,
                        mfaSettings.EmailOtpEnabled,
                        mfaSettings.SmsOtpEnabled,
                        mfaSettings.AuthenticatorSecret,
                        mfaSettings.IsMfaConfigured,
                        newBackupCodes,
                        cancellationToken);
                }
            }

            if (!isTotpValid && !isBackupValid)
            {
                return Failure(CommonResponse.CommonResponseErrorCodes.Unauthorized, "Invalid verification code");
            }

            var user = await authDAL.ValidateUserDB(userId, cancellationToken);
            if (user is null || !user.IsActive)
            {
                return Failure(CommonResponse.CommonResponseErrorCodes.Unauthorized, "User is inactive or not found");
            }

            var validUser = await GetAuthorizationUser(user, cancellationToken);
            var roles = GetRoles(validUser);
            var permissions = GetPermissions(validUser);
            var token = jwtTokenService.GenerateToken(user.Id, user.Email, roles, permissions);
            var refreshToken = GenerateRefreshToken();
            var refreshExpires = DateTime.UtcNow.AddDays(rememberMe ? jwtOptions.Value.RememberMeRefreshTokenExpiryDays : jwtOptions.Value.RefreshTokenExpiryDays);
            var refreshTokenHash = HashToken(refreshToken);

            await authDAL.SaveRefreshTokenDB(user.Id, refreshTokenHash, refreshExpires, cancellationToken);
            await authDAL.UpdateLastLoginDB(user.Id, cancellationToken);
            await authDAL.LogLoginHistoryDB(user.Id, user.Email, ipAddress, userAgent, true, null, cancellationToken);

            token.RefreshToken = refreshToken;
            token.RefreshTokenExpiresAtUtc = refreshExpires;
            token.User = ToUserDetails(user);
            token.Roles = roles;
            token.Permissions = permissions;

            return Success(token, "MFA login successful");
        }
        catch
        {
            return Failure(CommonResponse.CommonResponseErrorCodes.TechnicalError, "An error occurred during MFA verification");
        }
    }

    public async Task<Response<object>> Refresh(RefreshTokenRequestDTO request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Failure(CommonResponse.CommonResponseErrorCodes.InvalidRequest, "Refresh token is required");
        }

        var oldHash = HashToken(request.RefreshToken);
        var existingToken = await authDAL.GetRefreshTokenDB(oldHash, cancellationToken);
        if (existingToken is null || existingToken.IsDeleted || !existingToken.IsActive || existingToken.RevokedAtUtc.HasValue || existingToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return Failure(CommonResponse.CommonResponseErrorCodes.Unauthorized, "Invalid refresh token");
        }

        var user = await authDAL.ValidateUserDB(existingToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Failure(CommonResponse.CommonResponseErrorCodes.Unauthorized, "User is no longer active");
        }

        var validUser = await GetAuthorizationUser(user, cancellationToken);
        var roles = GetRoles(validUser);
        var permissions = GetPermissions(validUser);
        var token = jwtTokenService.GenerateToken(user.Id, user.Email, roles, permissions);
        var newRefreshToken = GenerateRefreshToken();
        var newRefreshHash = HashToken(newRefreshToken);
        var refreshExpires = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpiryDays);

        await authDAL.SaveRefreshTokenDB(user.Id, newRefreshHash, refreshExpires, cancellationToken);
        await authDAL.RevokeRefreshTokenDB(oldHash, newRefreshHash, user.Id, cancellationToken);

        token.RefreshToken = newRefreshToken;
        token.RefreshTokenExpiresAtUtc = refreshExpires;
        token.User = ToUserDetails(user);
        token.Roles = roles;
        token.Permissions = permissions;

        return Success(token, "Token refreshed");
    }

    public async Task<Response<object>> Logout(RefreshTokenRequestDTO request, int? userId, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            await authDAL.RevokeRefreshTokenDB(HashToken(request.RefreshToken), null, userId, cancellationToken);
        }

        return Success(new { LoggedOut = true }, "Logout successful");
    }

    private async Task<AuthUserModel> GetAuthorizationUser(AuthUserModel user, CancellationToken cancellationToken)
    {
        return await authDAL.ValidateUserDB(user.Id, cancellationToken) ?? user;
    }

    private static IReadOnlyList<string> GetRoles(AuthUserModel user)
    {
        return (user.Roles ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .DefaultIfEmpty("User")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> GetPermissions(AuthUserModel user)
    {
        var list = (user.Permissions ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (list.Count == 0)
        {
            list.AddRange(new[] { 
                "Dashboard.View", 
                "ChatAssistant.View", 
                "Document.Upload", 
                "KnowledgeBase.View", 
                "ChatHistory.View",
                "Users.View",
                "Roles.View",
                "Settings.View"
            });
        }

        return list;
    }

    private static string ValidateRegister(RegisterRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName)) return "Full name is required";
        if (string.IsNullOrWhiteSpace(request.Email)) return "Email is required";
        if (string.IsNullOrWhiteSpace(request.MobileNumber)) return "Mobile number is required";
        if (request.Password.Length < 8) return "Password must be at least 8 characters";
        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal)) return "Password and confirm password must match";
        return string.Empty;
    }

    private static UserDetailsDTO ToUserDetails(AuthUserModel user)
    {
        return new UserDetailsDTO
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            FullName = user.FullName,
            MobileNumber = user.MobileNumber,
            IsActive = user.IsActive
        };
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    private static Response<object> Success(object data, string message)
    {
        return new Response<object>
        {
            ReturnCode = (int)CommonResponse.CommonResponseErrorCodes.Success,
            ReturnMessage = message,
            ResponseTime = DateTime.UtcNow.ToString("O"),
            Data = data
        };
    }

    private static Response<object> Failure(CommonResponse.CommonResponseErrorCodes code, string message)
    {
        return new Response<object>
        {
            ReturnCode = (int)code,
            ReturnMessage = message,
            ResponseTime = DateTime.UtcNow.ToString("O")
        };
    }
}
