namespace AgenticKnowledgeAssistant.DTO.ResponseDTOs;

public sealed class StatusResponseDTO
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public object? Endpoints { get; set; }
}
