namespace AgenticKnowledgeAssistant.DTO.CommonDTOs;

public sealed class InvalidRequest
{
    public string ReturnCode { get; set; } = "400";
    public string ReturnMessage { get; set; } = "InvalidRequest";
}
