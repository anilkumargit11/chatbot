namespace AgenticKnowledgeAssistant.DTO.RequestDTOs;

public sealed class RagIndexRequestDTO
{
    public long? DocumentId { get; set; }
    public bool ForceReindex { get; set; }
}

public sealed class RagSearchRequestDTO
{
    public string Query { get; set; } = string.Empty;
    public IReadOnlyList<long> DocumentIds { get; set; } = Array.Empty<long>();
    public int TopK { get; set; } = 8;
    public string? Category { get; set; }
}

public sealed class RagChatRequestDTO
{
    public string Question { get; set; } = string.Empty;
    public Guid? SessionGuid { get; set; }
    public IReadOnlyList<long> DocumentIds { get; set; } = Array.Empty<long>();
    public int TopK { get; set; } = 8;
}
