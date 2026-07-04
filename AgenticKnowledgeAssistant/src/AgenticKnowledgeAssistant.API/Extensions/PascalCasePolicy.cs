namespace AgenticKnowledgeAssistant.API.Extensions;

public sealed class PascalCasePolicy : System.Text.Json.JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        return string.IsNullOrEmpty(name) ? name : char.ToUpperInvariant(name[0]) + name[1..];
    }
}
