using AgenticKnowledgeAssistant.DTO.ResponseDTOs;

namespace AgenticKnowledgeAssistant.BAL.Interfaces;

public interface IAgentBAL
{
    Task<ChatResponseDTO> HandleAgentRequest(AgenticKnowledgeAssistant.DTO.RequestDTOs.ChatRequestDTO request, CancellationToken cancellationToken = default);
    Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default);
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
