namespace AgenticKnowledgeAssistant.DTO.Models;

public sealed class RefreshTokenModel
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
}
