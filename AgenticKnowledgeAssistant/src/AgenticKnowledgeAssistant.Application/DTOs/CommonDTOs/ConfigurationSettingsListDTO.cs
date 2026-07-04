namespace AgenticKnowledgeAssistant.DTO.CommonDTOs;

public sealed class ConfigurationSettingsListDTO
{
    public string DefaultConnection { get; set; } = string.Empty;
    public string OpenAIEndpoint { get; set; } = "https://api.openai.com";
    public string OpenAIApiKey { get; set; } = string.Empty;
    public string JWT_Secret { get; set; } = string.Empty;
    public bool IsCheckAuthenticate { get; set; } = true;
    public int APIRateLimit { get; set; } = 120;
    public int APIRateLimitSeconds { get; set; } = 60;
    public int APILogTypeId { get; set; } = 1;
}
