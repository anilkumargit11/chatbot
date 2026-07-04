using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using MediatR;

namespace AgenticKnowledgeAssistant.Application.CQRS.Authentication.Commands.VerifyMfa;

public record VerifyMfaCommand(string MfaToken, string Code, bool RememberMe, string? IpAddress, string? UserAgent) : IRequest<Response<object>>;
