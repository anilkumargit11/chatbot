using System;

namespace AgenticKnowledgeAssistant.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Create, Update, Delete
    public string KeyValues { get; set; } = string.Empty;
    public string OldValues { get; set; } = string.Empty;
    public string NewValues { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
