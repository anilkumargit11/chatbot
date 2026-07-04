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
[Route("api/roles")]
public sealed class RolesController(IRoleAdminBAL roleAdminBAL) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRoles([FromQuery] string? search, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        if (!HasPermission("Roles.View")) return Forbid();
        var response = await roleAdminBAL.GetRoles(search, isActive, cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRole([FromRoute] int id, CancellationToken cancellationToken)
    {
        if (!HasPermission("Roles.View")) return Forbid();
        var response = await roleAdminBAL.GetRoleById(id, cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole(SaveRoleRequestDTO request, CancellationToken cancellationToken)
    {
        if (!HasPermission("Roles.Edit")) return Forbid();
        var response = await roleAdminBAL.CreateRole(request, GetCurrentUserId(), cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRole([FromRoute] int id, SaveRoleRequestDTO request, CancellationToken cancellationToken)
    {
        if (!HasPermission("Roles.Edit")) return Forbid();
        var response = await roleAdminBAL.UpdateRole(id, request, GetCurrentUserId(), cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRole([FromRoute] int id, CancellationToken cancellationToken)
    {
        if (!HasPermission("Roles.Edit")) return Forbid();
        var response = await roleAdminBAL.DeleteRole(id, GetCurrentUserId(), cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpGet("{id:int}/permissions")]
    public async Task<IActionResult> GetRolePermissions([FromRoute] int id, CancellationToken cancellationToken)
    {
        if (!HasPermission("Roles.View")) return Forbid();
        var response = await roleAdminBAL.GetPermissions(id, cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpPut("{id:int}/permissions")]
    public async Task<IActionResult> AssignPermissions([FromRoute] int id, AssignPermissionsRequestDTO request, CancellationToken cancellationToken)
    {
        if (!HasPermission("Roles.Edit")) return Forbid();
        var response = await roleAdminBAL.AssignPermissions(id, request, GetCurrentUserId(), cancellationToken);
        return StatusCode(ToHttpStatusCode(response.ReturnCode), response);
    }

    [HttpGet("/api/permissions")]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
    {
        if (!HasPermission("Roles.View")) return Forbid();
        var response = await roleAdminBAL.GetPermissions(null, cancellationToken);
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
