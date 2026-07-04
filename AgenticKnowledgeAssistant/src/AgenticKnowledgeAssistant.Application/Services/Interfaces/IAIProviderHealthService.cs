using AgenticKnowledgeAssistant.DTO.ResponseDTOs;

namespace AgenticKnowledgeAssistant.BAL.Interfaces;

public interface IAIProviderHealthService
{
    Task<IReadOnlyList<AIProviderHealthDTO>> CheckHealthAsync(CancellationToken cancellationToken = default);
    IReadOnlyList<AIProviderHealthDTO> GetConfigurationStatus();
}
