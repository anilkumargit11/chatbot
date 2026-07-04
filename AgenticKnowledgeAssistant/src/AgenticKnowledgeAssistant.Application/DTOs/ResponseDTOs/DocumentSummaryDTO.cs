namespace AgenticKnowledgeAssistant.DTO.ResponseDTOs;

public sealed class DocumentSummaryDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Preview { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
