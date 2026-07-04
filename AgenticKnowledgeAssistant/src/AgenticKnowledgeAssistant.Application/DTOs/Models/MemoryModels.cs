namespace AgenticKnowledgeAssistant.DTO.Models;

public sealed class MemoryCategoryModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class UserMemoryModel
{
    public long MemoryId { get; set; }
    public int UserId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsPinned { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public string? Metadata { get; set; }
}
