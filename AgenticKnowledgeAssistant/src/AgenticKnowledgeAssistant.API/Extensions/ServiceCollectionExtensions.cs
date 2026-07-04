using AgenticKnowledgeAssistant.API.Middlewares;
using AgenticKnowledgeAssistant.API.Services;
using AgenticKnowledgeAssistant.API.Filters;
using AgenticKnowledgeAssistant.BAL;
using AgenticKnowledgeAssistant.BAL.AIProviders;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.Common.JWT;
using AgenticKnowledgeAssistant.DAL;
using AgenticKnowledgeAssistant.DAL.Interfaces;

namespace AgenticKnowledgeAssistant.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<GlobalExceptionMiddleware>();
        services.AddScoped<JwtAuthenticationFilter>();
        services.AddScoped<ICommonBAL, CommonBAL>();
        services.AddScoped<IAuthBAL, AuthBAL>();
        services.AddScoped<IAgentBAL, AgentBAL>();
        services.AddScoped<IChatBAL, ChatBAL>();
        services.AddScoped<IConversationMemoryService, ConversationMemoryService>();
        services.AddScoped<ILongTermMemoryService, LongTermMemoryService>();
        services.AddScoped<IRagService, RagService>();
        services.AddScoped<IAIProviderHealthService, AIProviderHealthService>();
        services.AddScoped<IDocumentBAL, DocumentBAL>();
        services.AddScoped<IUserAdminBAL, UserAdminBAL>();
        services.AddScoped<IRoleAdminBAL, RoleAdminBAL>();
        services.AddScoped<IOcrService, OcrService>();
        services.AddScoped<ITranslatorService, TranslatorService>();
        services.AddSingleton<IImageContextService, ImageContextService>();
        services.AddScoped<AzureOpenAIProvider>();
        services.AddScoped<OpenAIProvider>();
        services.AddScoped<OllamaProvider>();
        services.AddScoped<LMStudioProvider>();
        services.AddScoped<LocalLlamaProvider>();
        services.AddScoped<AzureAIFoundryProvider>();
        services.AddScoped<IAIProvider>(provider => provider.GetRequiredService<AzureOpenAIProvider>());
        services.AddScoped<IAIProvider>(provider => provider.GetRequiredService<OpenAIProvider>());
        services.AddScoped<IAIProvider>(provider => provider.GetRequiredService<OllamaProvider>());
        services.AddScoped<IAIProvider>(provider => provider.GetRequiredService<LMStudioProvider>());
        services.AddScoped<IAIProvider>(provider => provider.GetRequiredService<AzureAIFoundryProvider>());
        services.AddScoped<IAIProvider>(provider => provider.GetRequiredService<LocalLlamaProvider>());
        services.AddScoped<IAIProviderResolver, AIProviderResolver>();

        services.AddScoped<ICommonDAL, CommonDAL>();
        services.AddScoped<IAuthDAL, AuthDAL>();
        services.AddScoped<IAgentDAL, AgentDAL>();
        services.AddScoped<IChatDAL, ChatDAL>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMemoryRepository, MemoryRepository>();
        services.AddScoped<IRagRepository, RagRepository>();
        services.AddScoped<IDocumentDAL, DocumentDAL>();
        services.AddScoped<IUserAdminDAL, UserAdminDAL>();
        services.AddScoped<IRoleAdminDAL, RoleAdminDAL>();
        services.AddScoped<IDatabaseAssistantDAL, DatabaseAssistantDAL>();

        services.AddScoped<AgenticKnowledgeAssistant.Security.Authentication.IMfaService, AgenticKnowledgeAssistant.Security.Authentication.MfaService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped(provider =>
        {
            var accessor = provider.GetRequiredService<IHttpContextAccessor>();
            return new BufferedCodeLogger(accessor, fireAndForget: false);
        });

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .SetIsOriginAllowed(origin =>
                        origin == "http://localhost:5173" ||
                        origin == "http://127.0.0.1:5173" ||
                        origin == "https://localhost:5173" ||
                        origin == "https://127.0.0.1:5173" ||
                        true)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
