using System.Threading;
using System.Threading.Tasks;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using MediatR;

namespace AgenticKnowledgeAssistant.Application.CQRS.Authentication.Commands.VerifyMfa;

public class VerifyMfaCommandHandler(IAuthBAL authBAL) : IRequestHandler<VerifyMfaCommand, Response<object>>
{
    public async Task<Response<object>> Handle(VerifyMfaCommand request, CancellationToken cancellationToken)
    {
        return await authBAL.VerifyMfaLogin(request.MfaToken, request.Code, request.RememberMe, request.IpAddress, request.UserAgent, cancellationToken);
    }
}
