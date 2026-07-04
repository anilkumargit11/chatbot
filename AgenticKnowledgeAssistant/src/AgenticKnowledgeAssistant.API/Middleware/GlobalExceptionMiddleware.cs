using AgenticKnowledgeAssistant.DTO.CommonDTOs;

namespace AgenticKnowledgeAssistant.API.Middlewares;

public sealed class GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (FluentValidation.ValidationException valEx)
        {
            logger.LogWarning(valEx, "Validation failed at {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new Response<object>
            {
                ReturnCode = (int)CommonResponse.CommonResponseErrorCodes.InvalidRequest,
                ReturnMessage = "Validation failed",
                Data = valEx.Errors.Select(e => new { property = e.PropertyName, error = e.ErrorMessage })
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception at {Path} | Message: {Message}", context.Request.Path, ex.Message);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new Response<object>
            {
                ReturnCode = (int)CommonResponse.CommonResponseErrorCodes.TechnicalError,
                ReturnMessage = "Technical Error",
                Data = new { detail = ex.Message, path = context.Request.Path.Value }
            });
        }
    }
}
