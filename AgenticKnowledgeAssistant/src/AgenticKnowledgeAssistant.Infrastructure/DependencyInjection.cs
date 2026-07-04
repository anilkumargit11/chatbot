using AgenticKnowledgeAssistant.Domain.Repositories;
using AgenticKnowledgeAssistant.Application.Interfaces;
using AgenticKnowledgeAssistant.Infrastructure.Cache;
using AgenticKnowledgeAssistant.Infrastructure.Persistence;
using AgenticKnowledgeAssistant.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgenticKnowledgeAssistant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? configuration.GetSection("ConnectionStrings")["DefaultConnection"];

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Redis Caching
        var redisConn = configuration.GetSection("Redis")["ConnectionString"];
        if (!string.IsNullOrEmpty(redisConn))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConn;
                options.InstanceName = "CopilotWorkspace_";
            });
        }
        else
        {
            services.AddDistributedMemoryCache(); // Fallback
        }

        services.AddScoped<IRedisCacheService, RedisCacheService>();

        return services;
    }
}
