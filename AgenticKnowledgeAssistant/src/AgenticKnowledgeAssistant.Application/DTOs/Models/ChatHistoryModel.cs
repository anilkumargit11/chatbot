namespace AgenticKnowledgeAssistant.DTO.Models;

public sealed class ChatHistoryModel
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
