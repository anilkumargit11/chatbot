namespace AgenticKnowledgeAssistant.DTO.RequestDTOs;

public sealed class SaveMemoryRequestDTO
{
    public string Category { get; set; } = "Reusable Context";
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public bool IsFavorite { get; set; }
    public string? Metadata { get; set; }
}

public sealed class UpdateMemoryRequestDTO
{
    public string? Category { get; set; }
    public string? Key { get; set; }
    public string? Value { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsPinned { get; set; }
    public bool? IsFavorite { get; set; }
    public string? Metadata { get; set; }
}

public sealed class MemorySearchRequestDTO
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsPinned { get; set; }
    public bool? IsFavorite { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
