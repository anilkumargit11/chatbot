namespace AgenticKnowledgeAssistant.DTO.RequestDTOs;

public sealed class CreateChatSessionRequestDTO
{
    public string? Title { get; set; }
}

public sealed class SaveChatMessageRequestDTO
{
    public Guid SessionGuid { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? Tokens { get; set; }
    public string? Metadata { get; set; }
}

public sealed class UpdateChatSessionRequestDTO
{
    public string? Title { get; set; }
    public bool? IsPinned { get; set; }
    public bool? IsFavorite { get; set; }
    public string? Status { get; set; }
}

public sealed class ConversationSearchRequestDTO
{
    public string? Search { get; set; }
    public bool? Pinned { get; set; }
    public bool? Favorite { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 30;
}
