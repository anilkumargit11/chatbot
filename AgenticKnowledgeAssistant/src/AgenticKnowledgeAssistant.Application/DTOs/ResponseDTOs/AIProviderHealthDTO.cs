namespace AgenticKnowledgeAssistant.DTO.ResponseDTOs;

public sealed class AIProviderHealthDTO
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Not Configured";
    public bool IsConfigured { get; set; }
    public bool IsConnected { get; set; }
    public bool IsActiveProvider { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public long LatencyMs { get; set; }
    public string? LastSuccessUtc { get; set; }
    public string? LastFailureUtc { get; set; }
    public string? FailureReason { get; set; }
}
