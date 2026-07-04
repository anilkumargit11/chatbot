namespace AgenticKnowledgeAssistant.DTO.ResponseDTOs;

public sealed class LoginResponseDTO
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public UserDetailsDTO? User { get; set; }
    public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
    public IEnumerable<string> Permissions { get; set; } = Array.Empty<string>();
    public bool IsMfaRequired { get; set; }
    public string MfaToken { get; set; } = string.Empty;
}
