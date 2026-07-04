using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgenticKnowledgeAssistant.BAL.AIProviders;

public sealed class AzureAIFoundryProvider : IAIProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<AIProviderOptions> _optionsMonitor;
    private readonly ILogger<AzureAIFoundryProvider> _logger;

    public AzureAIFoundryProvider(IHttpClientFactory httpClientFactory, IOptionsMonitor<AIProviderOptions> optionsMonitor, ILogger<AzureAIFoundryProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public string Name => "Azure AI Foundry";

    public bool IsConfigured =>
        Options.Enabled
        && OpenAIProvider.IsValid(Options.Endpoint)
        && OpenAIProvider.IsValid(Options.ApiKey)
        && OpenAIProvider.IsValid(Options.DeploymentName)
        && OpenAIProvider.IsValid(Options.ApiVersion);

    private AIProviderOptions ProviderOptions => _optionsMonitor.CurrentValue;
    private AzureAIFoundryProviderOptions Options => ProviderOptions.AzureAIFoundry;

    public async Task<string> GenerateChatCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Azure AI Foundry provider is not configured. EndpointConfigured={EndpointConfigured}, ApiKeyConfigured={ApiKeyConfigured}, DeploymentConfigured={DeploymentConfigured}",
                OpenAIProvider.IsValid(Options.Endpoint), OpenAIProvider.IsValid(Options.ApiKey), OpenAIProvider.IsValid(Options.DeploymentName));
            return AIProviderMessages.Unavailable;
        }

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(Options.Endpoint.TrimEnd('/'));
        client.Timeout = TimeSpan.FromSeconds(Math.Max(5, ProviderOptions.TimeoutSeconds));
        client.DefaultRequestHeaders.Add("api-key", Options.ApiKey);

        var payload = new
        {
            messages = new[] { new { role = "user", content = prompt } }
        };

        var path = $"/openai/deployments/{Uri.EscapeDataString(Options.DeploymentName)}/chat/completions?api-version={Uri.EscapeDataString(Options.ApiVersion)}";
        return await OpenAIProvider.PostChatCompletionAsync(client, path, payload, cancellationToken);
    }
}
