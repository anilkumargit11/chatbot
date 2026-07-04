using System;

namespace AgenticKnowledgeAssistant.Domain.Entities;

public class ChatSession : AuditableEntity
{
    public int Id { get; set; }
    public Guid SessionGuid { get; set; }
    public string Title { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int? FolderId { get; set; }
    public bool IsPinned { get; set; }
    public string? ShareGuid { get; set; }
}
