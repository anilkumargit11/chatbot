namespace AgenticKnowledgeAssistant.DTO.RequestDTOs;

public sealed class ChatRequestDTO
{
    public string Question { get; set; } = string.Empty;
    public string? OriginalQuestion { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string Mode { get; set; } = "Normal";
    public Guid? SessionGuid { get; set; }
    public string? AttachmentBase64 { get; set; }
    public string? AttachmentName { get; set; }
    public List<ChatAttachmentDTO> Attachments { get; set; } = new();
    public string? TargetLanguage { get; set; }
    public int? UserId { get; set; }
}

public sealed class ChatAttachmentDTO
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Base64Content { get; set; } = string.Empty;
    public long Size { get; set; }
}
