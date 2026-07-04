using System.Net.Http.Headers;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgenticKnowledgeAssistant.BAL.AIProviders;

public sealed class LMStudioProvider : IAIProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<AIProviderOptions> _optionsMonitor;
    private readonly ILogger<LMStudioProvider> _logger;

    public LMStudioProvider(IHttpClientFactory httpClientFactory, IOptionsMonitor<AIProviderOptions> optionsMonitor, ILogger<LMStudioProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public string Name => "LM Studio";

    public bool IsConfigured =>
        (Options.Enabled || ProviderOptions.AutoDetectLocalProviders)
        && OpenAIProvider.IsValid(Options.Endpoint)
        && OpenAIProvider.IsValid(Options.Model);

    private AIProviderOptions ProviderOptions => _optionsMonitor.CurrentValue;
    private LMStudioProviderOptions Options => ProviderOptions.LMStudio;

    public async Task<string> GenerateChatCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("LM Studio provider is not configured.");
            return AIProviderMessages.Unavailable;
        }

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(Options.Endpoint.TrimEnd('/'));
        client.Timeout = TimeSpan.FromSeconds(Math.Max(5, ProviderOptions.TimeoutSeconds));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "lm-studio");

        var payload = new
        {
            model = Options.Model,
            messages = new[] { new { role = "user", content = prompt } }
        };

        return await OpenAIProvider.PostChatCompletionAsync(client, "/v1/chat/completions", payload, cancellationToken);
    }
}
