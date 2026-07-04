namespace AgenticKnowledgeAssistant.DTO.Models;

public sealed class EmbeddingModel
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public string VectorData { get; set; } = string.Empty;
}
