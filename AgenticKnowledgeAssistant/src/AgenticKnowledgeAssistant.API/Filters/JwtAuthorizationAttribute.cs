using Microsoft.AspNetCore.Mvc;

namespace AgenticKnowledgeAssistant.API.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class JwtAuthorizationAttribute : TypeFilterAttribute
{
    public JwtAuthorizationAttribute() : base(typeof(JwtAuthenticationFilter))
    {
    }
}
