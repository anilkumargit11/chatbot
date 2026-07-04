namespace AgenticKnowledgeAssistant.Domain.Entities;

public class Folder : AuditableEntity
{
    public int Id { get; set; }
    public string FolderName { get; set; } = string.Empty;
    public int UserId { get; set; }
}
