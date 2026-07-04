namespace AgenticKnowledgeAssistant.DTO.Models;

public sealed class RegisterUserResultModel
{
    public int Id { get; set; }
    public bool IsDuplicateEmail { get; set; }
}
