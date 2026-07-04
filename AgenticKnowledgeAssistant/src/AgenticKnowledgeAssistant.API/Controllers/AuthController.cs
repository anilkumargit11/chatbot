using System.Security.Claims;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;
using AgenticKnowledgeAssistant.Application.CQRS.Authentication.Commands.Login;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgenticKnowledgeAssistant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthBAL authBAL, IMediator mediator) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [HttpPost("/api/RegisterUser")]
    public async Task<IActionResult> Register(RegisterRequestDTO request, CancellationToken cancellationToken)
    {
        var response = await authBAL.Register(request, cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [HttpPost("/api/LoginUser")]
    [HttpPost("/api/auth/token")]
    public async Task<IActionResult> Login(LoginRequestDTO request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var response = await mediator.Send(command, cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [HttpPost("refresh-token")]
    [HttpPost("/api/RefreshToken")]
    public async Task<IActionResult> Refresh(RefreshTokenRequestDTO request, CancellationToken cancellationToken)
    {
        var response = await authBAL.Refresh(request, cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [Authorize]
    [HttpPost("logout")]
    [HttpPost("/api/Logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequestDTO request, CancellationToken cancellationToken)
    {
        int? userId = int.TryParse(User.FindFirstValue("uid"), out var id) ? id : null;
        var response = await authBAL.Logout(request, userId, cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    private static int ToHttpStatusCode(int returnCode)
    {
        return returnCode is >= 100 and <= 599 ? returnCode : StatusCodes.Status400BadRequest;
    }
}
