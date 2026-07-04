using System.Net.Http.Headers;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgenticKnowledgeAssistant.BAL.AIProviders;

public sealed class LocalLlamaProvider : IAIProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<AIProviderOptions> _optionsMonitor;
    private readonly ILogger<LocalLlamaProvider> _logger;

    public LocalLlamaProvider(IHttpClientFactory httpClientFactory, IOptionsMonitor<AIProviderOptions> optionsMonitor, ILogger<LocalLlamaProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public string Name => "Local Llama";

    public bool IsConfigured =>
        (Options.Enabled || ProviderOptions.AutoDetectLocalProviders)
        && OpenAIProvider.IsValid(Options.Endpoint)
        && OpenAIProvider.IsValid(Options.Model);

    private AIProviderOptions ProviderOptions => _optionsMonitor.CurrentValue;
    private LocalLlamaProviderOptions Options => ProviderOptions.LocalLlama;

    public async Task<string> GenerateChatCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Local Llama provider is not configured.");
            return AIProviderMessages.Unavailable;
        }

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(Options.Endpoint.TrimEnd('/'));
        client.Timeout = TimeSpan.FromSeconds(Math.Max(5, ProviderOptions.TimeoutSeconds));
        if (OpenAIProvider.IsValid(Options.ApiKey))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Options.ApiKey);
        }

        var payload = new
        {
            model = Options.Model,
            messages = new[] { new { role = "user", content = prompt } }
        };

        return await OpenAIProvider.PostChatCompletionAsync(client, "/v1/chat/completions", payload, cancellationToken);
    }
}
