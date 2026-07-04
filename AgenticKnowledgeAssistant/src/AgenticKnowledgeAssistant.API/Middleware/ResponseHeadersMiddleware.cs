using System.Security.Cryptography;

namespace AgenticKnowledgeAssistant.API.Middlewares;

public sealed class ResponseHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public ResponseHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var nonceBytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(nonceBytes);
            var nonce = Convert.ToBase64String(nonceBytes);

            context.Response.Headers["Strict-Transport-Security"] = "max-age=63072000; includeSubDomains; preload";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Content-Security-Policy"] = $"base-uri 'self'; object-src 'none'; frame-ancestors 'none'; script-src 'self' 'nonce-{nonce}' 'strict-dynamic'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'; upgrade-insecure-requests;";
            context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            context.Response.Headers["Server"] = "None";
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
