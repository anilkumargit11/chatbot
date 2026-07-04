namespace AgenticKnowledgeAssistant.DTO.Models;

public sealed class RagDocumentModel
{
    public long DocumentId { get; set; }
    public int UserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProcessingStatus { get; set; } = "Uploaded";
    public int ChunkCount { get; set; }
    public int EmbeddingCount { get; set; }
    public string? Summary { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public sealed class RagChunkModel
{
    public long ChunkId { get; set; }
    public long DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public int? PageNumber { get; set; }
    public string Section { get; set; } = string.Empty;
    public string Heading { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int TokenCount { get; set; }
    public string? Metadata { get; set; }
}

public sealed class RagSearchResultModel
{
    public long DocumentId { get; set; }
    public long ChunkId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public int? PageNumber { get; set; }
    public string Section { get; set; } = string.Empty;
    public string Heading { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double KeywordScore { get; set; }
    public double VectorScore { get; set; }
    public double HybridScore { get; set; }
}
