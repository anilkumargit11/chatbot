namespace AgenticKnowledgeAssistant.BAL.Interfaces;

public interface IAIProviderResolver
{
    IAIProvider? GetProvider();
    IReadOnlyList<IAIProvider> GetProvidersInPriorityOrder();
}
