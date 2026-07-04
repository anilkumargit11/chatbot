using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace AgenticKnowledgeAssistant.API.Middlewares;

public sealed class JwtAuthenticationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var allowAnonymous = endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() is not null || IsAnonymousPath(context.Request.Path);

        if (!allowAnonymous && IsApiRequest(context) && string.IsNullOrWhiteSpace(context.Request.Headers.Authorization))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new Response<object>
            {
                ReturnCode = (int)CommonResponse.CommonResponseErrorCodes.Unauthorized,
                ReturnMessage = "Authorization token is required",
                ResponseTime = DateTime.UtcNow.ToString("O")
            }));
            return;
        }

        await next(context);
    }

    private static bool IsApiRequest(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAnonymousPath(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return value.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase)
            || value.Equals("/api/LoginUser", StringComparison.OrdinalIgnoreCase)
            || value.Equals("/api/RegisterUser", StringComparison.OrdinalIgnoreCase)
            || value.Equals("/api/RefreshToken", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/api/mfa/verify-login", StringComparison.OrdinalIgnoreCase)
            || value.Contains("/health", StringComparison.OrdinalIgnoreCase)
            || value.Equals("/api/status", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);
    }
}
