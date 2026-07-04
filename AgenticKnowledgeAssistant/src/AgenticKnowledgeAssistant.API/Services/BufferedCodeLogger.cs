using System.Text.Json;

namespace AgenticKnowledgeAssistant.API.Services;

public sealed class BufferedCodeLogger
{
    private readonly ILogger<BufferedCodeLogger>? _logger;
    private readonly bool _fireAndForget;
    private readonly List<(string Step, string Message)> _codeLogs = [];
    private readonly List<(string Step, string Message)> _errorLogs = [];
    private readonly string _methodName;

    public BufferedCodeLogger(IHttpContextAccessor accessor, bool fireAndForget = true)
    {
        _fireAndForget = fireAndForget;
        _logger = accessor.HttpContext?.RequestServices.GetService<ILogger<BufferedCodeLogger>>();
        _methodName = accessor.HttpContext?.Request.Path.Value ?? "Unknown";
    }

    public void Code(string step, string message)
    {
        _codeLogs.Add((step, message));
    }

    public void Code(string step, string message, object? response)
    {
        var serialized = response != null ? JsonSerializer.Serialize(response) : "null";
        _codeLogs.Add((step, $"{message} - Response: {serialized}"));
    }

    public void Error(Exception ex, string step = "")
    {
        _errorLogs.Add((step, ex.Message));
    }

    public void Flush()
    {
        void Write()
        {
            foreach (var log in _codeLogs)
            {
                _logger?.LogInformation("{MethodName} | {Step} | {Message}", _methodName, log.Step, log.Message);
            }

            foreach (var log in _errorLogs)
            {
                _logger?.LogError("{MethodName} | {Step} | {Message}", _methodName, log.Step, log.Message);
            }
        }

        if (_fireAndForget)
        {
            _ = Task.Run(Write);
            return;
        }

        Write();
    }
}
