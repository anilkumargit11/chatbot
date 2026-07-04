using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;
using MediatR;

namespace AgenticKnowledgeAssistant.Application.CQRS.Authentication.Commands.Login;

public record LoginCommand(LoginRequestDTO Request, string? IpAddress, string? UserAgent) : IRequest<Response<object>>;
