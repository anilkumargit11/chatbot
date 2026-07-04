using System.Threading;
using System.Threading.Tasks;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using MediatR;

namespace AgenticKnowledgeAssistant.Application.CQRS.Authentication.Commands.Login;

public class LoginCommandHandler(IAuthBAL authBAL) : IRequestHandler<LoginCommand, Response<object>>
{
    public async Task<Response<object>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return await authBAL.Login(request.Request, request.IpAddress, request.UserAgent, cancellationToken);
    }
}
