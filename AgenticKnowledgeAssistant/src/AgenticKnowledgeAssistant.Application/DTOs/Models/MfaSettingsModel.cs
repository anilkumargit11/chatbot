namespace AgenticKnowledgeAssistant.DTO.Models;

public sealed class MfaSettingsModel
{
    public int UserId { get; set; }
    public bool EmailOtpEnabled { get; set; }
    public bool SmsOtpEnabled { get; set; }
    public string? AuthenticatorSecret { get; set; }
    public bool IsMfaConfigured { get; set; }
    public string? BackupCodes { get; set; }
}
