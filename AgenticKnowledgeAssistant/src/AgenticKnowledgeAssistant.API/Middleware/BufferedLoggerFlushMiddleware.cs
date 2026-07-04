using AgenticKnowledgeAssistant.API.Services;

namespace AgenticKnowledgeAssistant.API.Middlewares;

public sealed class BufferedLoggerFlushMiddleware
{
    private readonly RequestDelegate _next;

    public BufferedLoggerFlushMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, BufferedCodeLogger logger)
    {
        try
        {
            await _next(context);
        }
        finally
        {
            logger.Flush();
        }
    }
}
