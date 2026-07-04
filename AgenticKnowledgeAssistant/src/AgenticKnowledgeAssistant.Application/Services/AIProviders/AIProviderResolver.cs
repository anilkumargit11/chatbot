using AgenticKnowledgeAssistant.BAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgenticKnowledgeAssistant.BAL.AIProviders;

public sealed class AIProviderResolver : IAIProviderResolver
{
    private readonly IEnumerable<IAIProvider> _providers;
    private readonly ILogger<AIProviderResolver> _logger;

    public AIProviderResolver(IEnumerable<IAIProvider> providers, ILogger<AIProviderResolver> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public IAIProvider? GetProvider()
    {
        var provider = GetProvidersInPriorityOrder().FirstOrDefault(provider => provider.IsConfigured);
        if (provider is null)
        {
            _logger.LogWarning("No AI provider is configured. Checked providers: {Providers}", string.Join(", ", _providers.Select(provider => provider.Name)));
            return null;
        }

        _logger.LogInformation("Selected AI provider: {ProviderName}", provider.Name);
        return provider;
    }

    public IReadOnlyList<IAIProvider> GetProvidersInPriorityOrder()
    {
        return _providers.ToArray();
    }
}
