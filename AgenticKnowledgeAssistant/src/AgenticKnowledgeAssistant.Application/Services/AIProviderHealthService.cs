using System.Diagnostics;
using AgenticKnowledgeAssistant.BAL.AIProviders;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using AgenticKnowledgeAssistant.DTO.ResponseDTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgenticKnowledgeAssistant.BAL;

public sealed class AIProviderHealthService : IAIProviderHealthService
{
    private const string HealthPrompt = "Reply with the single word: Connected";
    private readonly IAIProviderResolver _providerResolver;
    private readonly IOptionsMonitor<AIProviderOptions> _optionsMonitor;
    private readonly ILogger<AIProviderHealthService> _logger;

    public AIProviderHealthService(
        IAIProviderResolver providerResolver,
        IOptionsMonitor<AIProviderOptions> optionsMonitor,
        ILogger<AIProviderHealthService> logger)
    {
        _providerResolver = providerResolver;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public IReadOnlyList<AIProviderHealthDTO> GetConfigurationStatus()
    {
        var providers = _providerResolver.GetProvidersInPriorityOrder();
        var activeProvider = providers.FirstOrDefault(provider => provider.IsConfigured)?.Name;
        return providers.Select(provider =>
        {
            var health = BuildBaseHealth(provider, activeProvider);
            health.Status = provider.IsConfigured ? "Configured" : "Not Configured";
            return health;
        }).ToArray();
    }

    public async Task<IReadOnlyList<AIProviderHealthDTO>> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var providers = _providerResolver.GetProvidersInPriorityOrder();
        var results = new List<AIProviderHealthDTO>(providers.Count);
        string? activeProvider = null;

        foreach (var provider in providers)
        {
            var health = BuildBaseHealth(provider, activeProvider);
            if (!provider.IsConfigured)
            {
                health.Status = "Not Configured";
                health.FailureReason = "Required endpoint, model/deployment, or API key is missing.";
                results.Add(health);
                continue;
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var answer = await provider.GenerateChatCompletionAsync(HealthPrompt, cancellationToken);
                stopwatch.Stop();
                health.LatencyMs = stopwatch.ElapsedMilliseconds;

                if (!string.IsNullOrWhiteSpace(answer) && !answer.Equals(AIProviderMessages.Unavailable, StringComparison.Ordinal))
                {
                    health.Status = "Connected";
                    health.IsConnected = true;
                    health.LastSuccessUtc = DateTime.UtcNow.ToString("O");
                    if (activeProvider is null)
                    {
                        activeProvider = provider.Name;
                        health.IsActiveProvider = true;
                    }
                }
                else
                {
                    health.Status = "Disconnected";
                    health.LastFailureUtc = DateTime.UtcNow.ToString("O");
                    health.FailureReason = "Provider returned an empty or unusable response.";
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                health.LatencyMs = stopwatch.ElapsedMilliseconds;
                health.Status = "Disconnected";
                health.LastFailureUtc = DateTime.UtcNow.ToString("O");
                health.FailureReason = SanitizeFailure(ex);
                _logger.LogWarning(ex, "AI provider health check failed. Provider={ProviderName}", provider.Name);
            }

            results.Add(health);
        }

        return results;
    }

    private AIProviderHealthDTO BuildBaseHealth(IAIProvider provider, string? activeProvider)
    {
        var metadata = GetProviderMetadata(provider.Name);
        return new AIProviderHealthDTO
        {
            Name = provider.Name,
            IsConfigured = provider.IsConfigured,
            IsActiveProvider = provider.Name.Equals(activeProvider, StringComparison.OrdinalIgnoreCase),
            Endpoint = metadata.Endpoint,
            Model = metadata.Model,
            DeploymentName = metadata.DeploymentName
        };
    }

    private (string Endpoint, string Model, string DeploymentName) GetProviderMetadata(string providerName)
    {
        var options = _optionsMonitor.CurrentValue;
        return providerName switch
        {
            "Azure OpenAI" => (options.AzureOpenAI.Endpoint, string.Empty, options.AzureOpenAI.DeploymentName),
            "OpenAI" => (options.OpenAI.Endpoint, options.OpenAI.Model, string.Empty),
            "Ollama" => (options.Ollama.Endpoint, options.Ollama.Model, string.Empty),
            "LM Studio" => (options.LMStudio.Endpoint, options.LMStudio.Model, string.Empty),
            "Azure AI Foundry" => (options.AzureAIFoundry.Endpoint, string.Empty, options.AzureAIFoundry.DeploymentName),
            "Local Llama" => (options.LocalLlama.Endpoint, options.LocalLlama.Model, string.Empty),
            _ => (string.Empty, string.Empty, string.Empty)
        };
    }

    private static string SanitizeFailure(Exception ex)
    {
        var message = ex.GetBaseException().Message;
        return string.IsNullOrWhiteSpace(message)
            ? ex.GetType().Name
            : message.Replace(Environment.NewLine, " ").Trim();
    }
}
