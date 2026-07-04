namespace AgenticKnowledgeAssistant.Common.JWT;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "AgenticKnowledgeAssistant";
    public string Audience { get; set; } = "AgenticKnowledgeAssistant.Client";
    public string SigningKey { get; set; } = "CHANGE_THIS_TO_A_32_CHARACTER_MINIMUM_SECRET";
    public int ExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 7;
    public int RememberMeRefreshTokenExpiryDays { get; set; } = 30;
}
