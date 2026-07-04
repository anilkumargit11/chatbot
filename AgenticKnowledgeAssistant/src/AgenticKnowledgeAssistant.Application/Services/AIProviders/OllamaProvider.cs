using System.Text;
using System.Text.Json;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgenticKnowledgeAssistant.BAL.AIProviders;

public sealed class OllamaProvider : IAIProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<AIProviderOptions> _optionsMonitor;
    private readonly ILogger<OllamaProvider> _logger;

    public OllamaProvider(IHttpClientFactory httpClientFactory, IOptionsMonitor<AIProviderOptions> optionsMonitor, ILogger<OllamaProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public string Name => "Ollama";

    public bool IsConfigured =>
        (Options.Enabled || ProviderOptions.AutoDetectLocalProviders)
        && OpenAIProvider.IsValid(Options.Endpoint)
        && OpenAIProvider.IsValid(Options.Model);

    private AIProviderOptions ProviderOptions => _optionsMonitor.CurrentValue;
    private OllamaProviderOptions Options => ProviderOptions.Ollama;

    public async Task<string> GenerateChatCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Ollama provider is not configured. EndpointConfigured={EndpointConfigured}, ModelConfigured={ModelConfigured}",
                OpenAIProvider.IsValid(Options.Endpoint), OpenAIProvider.IsValid(Options.Model));
            return AIProviderMessages.Unavailable;
        }

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(Options.Endpoint.TrimEnd('/'));
        client.Timeout = TimeSpan.FromSeconds(Math.Max(5, ProviderOptions.TimeoutSeconds));

        var payload = new
        {
            model = Options.Model,
            prompt,
            stream = false
        };

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/api/generate", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var json = JsonDocument.Parse(body);
        return json.RootElement.TryGetProperty("response", out var answer) ? answer.GetString() ?? string.Empty : string.Empty;
    }
}
