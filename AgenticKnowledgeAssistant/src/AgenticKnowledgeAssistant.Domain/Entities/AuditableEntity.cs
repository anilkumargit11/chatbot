namespace AgenticKnowledgeAssistant.Domain.Entities;

public abstract class AuditableEntity
{
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedDate { get; set; }
    public string? LastModifiedBy { get; set; }
}
