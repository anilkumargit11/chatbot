using AgenticKnowledgeAssistant.DTO.Models;

namespace AgenticKnowledgeAssistant.DTO.ResponseDTOs;

public sealed class RagUploadResponseDTO
{
    public long DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ChunkCount { get; set; }
}

public sealed class RagSearchResponseDTO
{
    public IReadOnlyList<RagSearchResultModel> Results { get; set; } = Array.Empty<RagSearchResultModel>();
}

public sealed class RagChatResponseDTO
{
    public string Answer { get; set; } = string.Empty;
    public IReadOnlyList<RagSearchResultModel> Sources { get; set; } = Array.Empty<RagSearchResultModel>();
    public double ConfidenceScore { get; set; }
}
