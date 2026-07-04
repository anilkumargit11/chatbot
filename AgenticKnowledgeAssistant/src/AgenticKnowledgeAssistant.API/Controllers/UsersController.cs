using System.Security.Claims;
using AgenticKnowledgeAssistant.API.Filters;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgenticKnowledgeAssistant.API.Controllers;

[ApiController]
[Authorize]
[JwtAuthorization]
[Route("api/users")]
public sealed class UsersController(IUserAdminBAL userAdminBAL) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] int? roleId, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        if (!HasPermission("Users.View")) return Forbid();
        var response = await userAdminBAL.GetUsers(search, roleId, isActive, cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser([FromRoute] int id, CancellationToken cancellationToken)
    {
        if (!HasPermission("Users.View")) return Forbid();
        var response = await userAdminBAL.GetUserById(id, cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(SaveUserRequestDTO request, CancellationToken cancellationToken)
    {
        if (!HasPermission("Users.Create")) return Forbid();
        var response = await userAdminBAL.CreateUser(request, GetCurrentUserId(), cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser([FromRoute] int id, SaveUserRequestDTO request, CancellationToken cancellationToken)
    {
        if (!HasPermission("Users.Edit")) return Forbid();
        var response = await userAdminBAL.UpdateUser(id, request, GetCurrentUserId(), cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser([FromRoute] int id, CancellationToken cancellationToken)
    {
        if (!HasPermission("Users.Delete")) return Forbid();
        var response = await userAdminBAL.DeleteUser(id, GetCurrentUserId(), cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpPut("{id:int}/activate")]
    public async Task<IActionResult> ActivateUser([FromRoute] int id, CancellationToken cancellationToken)
    {
        if (!HasPermission("Users.Edit")) return Forbid();
        var response = await userAdminBAL.SetUserStatus(id, true, GetCurrentUserId(), cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpPut("{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateUser([FromRoute] int id, CancellationToken cancellationToken)
    {
        if (!HasPermission("Users.Edit")) return Forbid();
        var response = await userAdminBAL.SetUserStatus(id, false, GetCurrentUserId(), cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpPut("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword([FromRoute] int id, ResetPasswordRequestDTO request, CancellationToken cancellationToken)
    {
        if (!HasPermission("Users.Edit")) return Forbid();
        var response = await userAdminBAL.ResetPassword(id, request, GetCurrentUserId(), cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    private int GetCurrentUserId()
    {
        return int.TryParse(User.FindFirstValue("uid"), out var id) ? id : 0;
    }

    private bool HasPermission(string permission)
    {
        return User.Claims
            .Where(claim => claim.Type == "permission")
            .Any(claim => string.Equals(claim.Value, permission, StringComparison.OrdinalIgnoreCase));
    }

    private static int ToHttpStatusCode(int returnCode)
    {
        return returnCode is >= 100 and <= 599 ? returnCode : StatusCodes.Status400BadRequest;
    }
}
