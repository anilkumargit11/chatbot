using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgenticKnowledgeAssistant.BAL.AIProviders;

public sealed class OpenAIProvider : IAIProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<AIProviderOptions> _optionsMonitor;
    private readonly ILogger<OpenAIProvider> _logger;

    public OpenAIProvider(IHttpClientFactory httpClientFactory, IOptionsMonitor<AIProviderOptions> optionsMonitor, ILogger<OpenAIProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public string Name => "OpenAI";

    public bool IsConfigured =>
        Options.Enabled
        && IsValid(Options.Endpoint)
        && IsValid(Options.ApiKey)
        && IsValid(Options.Model);

    private AIProviderOptions ProviderOptions => _optionsMonitor.CurrentValue;
    private OpenAIProviderOptions Options => ProviderOptions.OpenAI;

    public async Task<string> GenerateChatCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("OpenAI provider is not configured. EndpointConfigured={EndpointConfigured}, ApiKeyConfigured={ApiKeyConfigured}, ModelConfigured={ModelConfigured}",
                IsValid(Options.Endpoint), IsValid(Options.ApiKey), IsValid(Options.Model));
            return AIProviderMessages.Unavailable;
        }

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(Options.Endpoint.TrimEnd('/'));
        client.Timeout = TimeSpan.FromSeconds(Math.Max(5, ProviderOptions.TimeoutSeconds));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Options.ApiKey);

        var payload = new
        {
            model = Options.Model,
            messages = new[] { new { role = "user", content = prompt } }
        };

        return await PostChatCompletionAsync(client, "/v1/chat/completions", payload, cancellationToken);
    }

    internal static async Task<string> PostChatCompletionAsync(HttpClient client, string path, object payload, CancellationToken cancellationToken)
    {
        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(path, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var json = JsonDocument.Parse(body);
        return json.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
    }

    internal static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && !value.Contains("YOUR_", StringComparison.OrdinalIgnoreCase)
            && !value.Contains("CHANGE_THIS", StringComparison.OrdinalIgnoreCase);
    }
}

public static class AIProviderMessages
{
    public const string Unavailable = "General AI is currently unavailable because no AI provider is configured or reachable. Please configure an AI provider in Settings or try again later.";
    public const string ConfiguredProviderFailed = "General AI is currently unavailable because no AI provider is configured or reachable. Please configure an AI provider in Settings or try again later.";
}
