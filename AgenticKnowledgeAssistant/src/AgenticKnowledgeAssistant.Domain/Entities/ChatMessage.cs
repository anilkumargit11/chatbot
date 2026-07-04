namespace AgenticKnowledgeAssistant.Domain.Entities;

public class ChatMessage : AuditableEntity
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string Role { get; set; } = string.Empty; // user or assistant
    public string Content { get; set; } = string.Empty;
    public double? ConfidenceScore { get; set; }
    public int? LatencyMs { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public int? TotalTokens { get; set; }
}
