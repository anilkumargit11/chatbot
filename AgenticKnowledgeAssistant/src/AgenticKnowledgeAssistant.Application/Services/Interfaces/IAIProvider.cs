namespace AgenticKnowledgeAssistant.BAL.Interfaces;

public interface IAIProvider
{
    string Name { get; }
    bool IsConfigured { get; }
    Task<string> GenerateChatCompletionAsync(string prompt, CancellationToken cancellationToken = default);
}
