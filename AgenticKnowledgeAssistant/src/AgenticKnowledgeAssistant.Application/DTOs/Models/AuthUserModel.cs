namespace AgenticKnowledgeAssistant.DTO.Models;

public sealed class AuthUserModel
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Roles { get; set; } = string.Empty;
    public string Permissions { get; set; } = string.Empty;
}
