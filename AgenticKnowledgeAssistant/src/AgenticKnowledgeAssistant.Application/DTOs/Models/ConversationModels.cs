namespace AgenticKnowledgeAssistant.DTO.Models;

public sealed class ConversationSessionModel
{
    public int Id { get; set; }
    public Guid SessionGuid { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public bool IsPinned { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public int MessageCount { get; set; }
    public string? LastMessagePreview { get; set; }
}

public sealed class ConversationMessageModel
{
    public long MessageId { get; set; }
    public Guid SessionGuid { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? Tokens { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? Metadata { get; set; }
}
