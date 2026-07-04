using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgenticKnowledgeAssistant.BAL.AIProviders;

public sealed class AzureOpenAIProvider : IAIProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<AIProviderOptions> _optionsMonitor;
    private readonly ILogger<AzureOpenAIProvider> _logger;

    public AzureOpenAIProvider(IHttpClientFactory httpClientFactory, IOptionsMonitor<AIProviderOptions> optionsMonitor, ILogger<AzureOpenAIProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public string Name => "Azure OpenAI";

    public bool IsConfigured =>
        Options.Enabled
        && OpenAIProvider.IsValid(Options.Endpoint)
        && OpenAIProvider.IsValid(Options.ApiKey)
        && OpenAIProvider.IsValid(Options.DeploymentName)
        && OpenAIProvider.IsValid(Options.ApiVersion);

    private AIProviderOptions ProviderOptions => _optionsMonitor.CurrentValue;
    private AzureOpenAIProviderOptions Options => ProviderOptions.AzureOpenAI;

    public async Task<string> GenerateChatCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Azure OpenAI provider is not configured. EndpointConfigured={EndpointConfigured}, ApiKeyConfigured={ApiKeyConfigured}, DeploymentConfigured={DeploymentConfigured}",
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
