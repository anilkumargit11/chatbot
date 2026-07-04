using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;

namespace AgenticKnowledgeAssistant.BAL.Interfaces;

public interface IAuthBAL
{
    Task<Response<object>> Register(RegisterRequestDTO request, CancellationToken cancellationToken = default);
    Task<Response<object>> Login(LoginRequestDTO request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<Response<object>> Refresh(RefreshTokenRequestDTO request, CancellationToken cancellationToken = default);
    Task<Response<object>> Logout(RefreshTokenRequestDTO request, int? userId, CancellationToken cancellationToken = default);
    Task<Response<object>> VerifyMfaLogin(string mfaToken, string code, bool rememberMe, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
}
