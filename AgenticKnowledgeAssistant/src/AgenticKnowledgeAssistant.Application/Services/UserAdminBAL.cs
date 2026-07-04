using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.Common.Extensions;
using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using AgenticKnowledgeAssistant.DTO.Models;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;
using AgenticKnowledgeAssistant.Security.Encryption;
using System.Text.RegularExpressions;

namespace AgenticKnowledgeAssistant.BAL;

public sealed class UserAdminBAL(IUserAdminDAL userAdminDAL, ICommonBAL commonBAL) : IUserAdminBAL
{
    public async Task<Response<object>> GetUsers(string? search, int? roleId, bool? isActive, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var users = await userAdminDAL.GetUsersDB(search, roleId, isActive, cancellationToken);
        return commonBAL.Success(users, startTime);
    }

    public async Task<Response<object>> GetUserById(int id, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var user = await userAdminDAL.GetUserByIdDB(id, cancellationToken);
        return user is null
            ? commonBAL.Failure(404, "User not found", startTime)
            : commonBAL.Success(user, startTime);
    }

    public async Task<Response<object>> CreateUser(SaveUserRequestDTO request, int currentUserId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var validation = ValidateUser(request, requirePassword: true);
        if (!string.IsNullOrWhiteSpace(validation))
        {
            return commonBAL.Failure(400, validation, startTime);
        }

        var userId = await userAdminDAL.CreateUserDB(ToModel(request), PasswordHasher.Hash(request.Password), currentUserId, cancellationToken);
        return userId <= 0
            ? commonBAL.Failure(409, "User name/email already exists or selected role is invalid", startTime)
            : commonBAL.Success(new { id = userId }, startTime);
    }

    public async Task<Response<object>> UpdateUser(int id, SaveUserRequestDTO request, int currentUserId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var validation = ValidateUser(request, requirePassword: false);
        if (!string.IsNullOrWhiteSpace(validation))
        {
            return commonBAL.Failure(400, validation, startTime);
        }

        var rows = await userAdminDAL.UpdateUserDB(id, ToModel(request), currentUserId, cancellationToken);
        return rows <= 0
            ? commonBAL.Failure(404, "User not found, duplicate user name/email, or selected role is invalid", startTime)
            : commonBAL.Success(new { id }, startTime);
    }

    public async Task<Response<object>> DeleteUser(int id, int currentUserId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var rows = await userAdminDAL.DeleteUserDB(id, currentUserId, cancellationToken);
        return rows <= 0
            ? commonBAL.Failure(404, "User not found", startTime)
            : commonBAL.Success(new { id }, startTime);
    }

    public async Task<Response<object>> SetUserStatus(int id, bool isActive, int currentUserId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var rows = await userAdminDAL.SetUserStatusDB(id, isActive, currentUserId, cancellationToken);
        return rows <= 0
            ? commonBAL.Failure(404, "User not found", startTime)
            : commonBAL.Success(new { id, isActive }, startTime);
    }

    public async Task<Response<object>> ResetPassword(int id, ResetPasswordRequestDTO request, int currentUserId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return commonBAL.Failure(400, "Password must be at least 8 characters", startTime);
        }

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return commonBAL.Failure(400, "Password and confirm password must match", startTime);
        }

        var rows = await userAdminDAL.ResetPasswordDB(id, PasswordHasher.Hash(request.Password), currentUserId, cancellationToken);
        return rows <= 0
            ? commonBAL.Failure(404, "User not found", startTime)
            : commonBAL.Success(new { id }, startTime);
    }

    private static AdminUserModel ToModel(SaveUserRequestDTO request)
    {
        return new AdminUserModel
        {
            UserName = request.UserName.Trim(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            MobileNumber = request.MobileNumber.Trim(),
            RoleId = request.RoleId,
            IsActive = request.IsActive
        };
    }

    private static string ValidateUser(SaveUserRequestDTO request, bool requirePassword)
    {
        if (string.IsNullOrWhiteSpace(request.UserName)) return "User name is required";
        if (string.IsNullOrWhiteSpace(request.FullName)) return "Full name is required";
        if (string.IsNullOrWhiteSpace(request.Email)) return "Email is required";
        if (request.RoleId <= 0) return "Role is required";
        if (!string.IsNullOrWhiteSpace(request.MobileNumber) && !Regex.IsMatch(request.MobileNumber.Trim(), "^[0-9]{10,15}$")) return "Mobile number must contain 10 to 15 digits";
        if (requirePassword && (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)) return "Password must be at least 8 characters";
        if (requirePassword && !string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal)) return "Password and confirm password must match";
        return string.Empty;
    }
}
