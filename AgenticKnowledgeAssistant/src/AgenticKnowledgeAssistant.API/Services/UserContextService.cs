namespace AgenticKnowledgeAssistant.API.Services;

public sealed class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserToken => _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty) ?? string.Empty;
    public string UserKey => _httpContextAccessor.HttpContext?.User?.FindFirst("upk")?.Value ?? string.Empty;
}
