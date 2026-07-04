using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AgenticKnowledgeAssistant.API.Filters;

public sealed class JwtAuthenticationFilter : IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var allowAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
        if (allowAnonymous)
        {
            return Task.CompletedTask;
        }

        var isAuthenticated = context.HttpContext.User.Identity?.IsAuthenticated == true;
        if (!isAuthenticated)
        {
            context.Result = new UnauthorizedObjectResult(new Response<object>
            {
                ReturnCode = (int)CommonResponse.CommonResponseErrorCodes.Unauthorized,
                ReturnMessage = "Invalid or expired JWT token",
                ResponseTime = DateTime.UtcNow.ToString("O")
            });
        }

        return Task.CompletedTask;
    }
}
