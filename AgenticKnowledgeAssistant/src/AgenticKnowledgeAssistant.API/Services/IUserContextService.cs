namespace AgenticKnowledgeAssistant.API.Services;

public interface IUserContextService
{
    string UserToken { get; }
    string UserKey { get; }
}
