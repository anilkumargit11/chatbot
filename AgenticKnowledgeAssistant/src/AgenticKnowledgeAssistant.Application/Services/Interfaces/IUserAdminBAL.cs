using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;

namespace AgenticKnowledgeAssistant.BAL.Interfaces;

public interface IUserAdminBAL
{
    Task<Response<object>> GetUsers(string? search, int? roleId, bool? isActive, CancellationToken cancellationToken = default);
    Task<Response<object>> GetUserById(int id, CancellationToken cancellationToken = default);
    Task<Response<object>> CreateUser(SaveUserRequestDTO request, int currentUserId, CancellationToken cancellationToken = default);
    Task<Response<object>> UpdateUser(int id, SaveUserRequestDTO request, int currentUserId, CancellationToken cancellationToken = default);
    Task<Response<object>> DeleteUser(int id, int currentUserId, CancellationToken cancellationToken = default);
    Task<Response<object>> SetUserStatus(int id, bool isActive, int currentUserId, CancellationToken cancellationToken = default);
    Task<Response<object>> ResetPassword(int id, ResetPasswordRequestDTO request, int currentUserId, CancellationToken cancellationToken = default);
}
