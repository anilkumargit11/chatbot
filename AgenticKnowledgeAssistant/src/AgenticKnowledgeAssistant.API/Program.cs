using System.Text;
using System.Threading.RateLimiting;
using AgenticKnowledgeAssistant.API.Extensions;
using AgenticKnowledgeAssistant.API.Middlewares;
using AgenticKnowledgeAssistant.Common.JWT;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;
using Serilog;
using AgenticKnowledgeAssistant.Application;
using AgenticKnowledgeAssistant.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("ai-provider-settings.local.json", optional: true, reloadOnChange: true);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Host.UseSerilog();

var appSettings = builder.Configuration
    .GetSection("AppSettings")
    .Get<ConfigurationSettingsListDTO>()
    ?? throw new InvalidOperationException("AppSettings configuration is missing");

if (string.IsNullOrWhiteSpace(appSettings.DefaultConnection))
{
    appSettings.DefaultConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
}

if (string.IsNullOrWhiteSpace(appSettings.OpenAIApiKey))
{
    appSettings.OpenAIApiKey = builder.Configuration["OpenAI:ApiKey"] ?? string.Empty;
}

if (string.IsNullOrWhiteSpace(appSettings.OpenAIEndpoint))
{
    appSettings.OpenAIEndpoint = builder.Configuration["OpenAI:Endpoint"] ?? "https://api.openai.com";
}

builder.Services.AddSingleton(appSettings);
builder.Services.Configure<AIProviderOptions>(builder.Configuration.GetSection("AIProviders"));
builder.Services.PostConfigure<AIProviderOptions>(options =>
{
    var meetingSummaryEndpoint = builder.Configuration["MEETING_SUMMARY_SERVICE_URL"]
        ?? builder.Configuration["MeetingSummary:ServiceUrl"];
    var meetingSummaryModel = builder.Configuration["MEETING_SUMMARY_MODEL"]
        ?? builder.Configuration["MeetingSummary:Model"];
    var meetingSummaryApiKey = builder.Configuration["MEETING_SUMMARY_API_KEY"]
        ?? builder.Configuration["MeetingSummary:ApiKey"];
    var meetingSummaryTimeout = builder.Configuration["MEETING_SUMMARY_TIMEOUT_SECONDS"]
        ?? builder.Configuration["MeetingSummary:TimeoutSeconds"];

    if (!string.IsNullOrWhiteSpace(meetingSummaryEndpoint))
    {
        options.LocalLlama.Enabled = true;
        options.LocalLlama.Endpoint = meetingSummaryEndpoint;
        options.AutoDetectLocalProviders = false;

        if (options.TimeoutSeconds < 60)
        {
            options.TimeoutSeconds = 60;
        }
    }

    if (!string.IsNullOrWhiteSpace(meetingSummaryModel))
    {
        options.LocalLlama.Model = meetingSummaryModel;
    }

    if (!string.IsNullOrWhiteSpace(meetingSummaryApiKey))
    {
        options.LocalLlama.ApiKey = meetingSummaryApiKey;
    }

    if (int.TryParse(meetingSummaryTimeout, out var localProviderTimeoutSeconds) && localProviderTimeoutSeconds >= 5)
    {
        options.TimeoutSeconds = localProviderTimeoutSeconds;
    }

    if (string.IsNullOrWhiteSpace(options.OpenAI.ApiKey))
    {
        options.OpenAI.ApiKey = builder.Configuration["OpenAI:ApiKey"]
            ?? builder.Configuration["AppSettings:OpenAIApiKey"]
            ?? string.Empty;
    }

    if (string.IsNullOrWhiteSpace(options.OpenAI.Endpoint))
    {
        options.OpenAI.Endpoint = builder.Configuration["OpenAI:Endpoint"]
            ?? builder.Configuration["AppSettings:OpenAIEndpoint"]
            ?? "https://api.openai.com";
    }

    if (string.IsNullOrWhiteSpace(options.OpenAI.Model))
    {
        options.OpenAI.Model = builder.Configuration["OpenAI:Model"] ?? "gpt-4o-mini";
    }
});
builder.Services.Configure<JwtOptions>(options =>
{
    options.Issuer = builder.Configuration["Jwt:Issuer"] ?? "AgenticKnowledgeAssistant";
    options.Audience = builder.Configuration["Jwt:Audience"] ?? "AgenticKnowledgeAssistant.Client";
    options.SigningKey = appSettings.JWT_Secret;
    options.ExpiryMinutes = int.TryParse(builder.Configuration["Jwt:ExpiryMinutes"], out var expiryMinutes) ? expiryMinutes : 60;
    options.RefreshTokenExpiryDays = int.TryParse(builder.Configuration["Jwt:RefreshTokenExpiryDays"], out var refreshDays) ? refreshDays : 7;
    options.RememberMeRefreshTokenExpiryDays = int.TryParse(builder.Configuration["Jwt:RememberMeRefreshTokenExpiryDays"], out var rememberDays) ? rememberDays : 30;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 1000;
    options.Limits.MaxConcurrentUpgradedConnections = 1000;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = new PascalCasePolicy();
        options.JsonSerializerOptions.DictionaryKeyPolicy = new PascalCasePolicy();
    });

builder.Services.AddCorsPolicy();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Agentic Knowledge Assistant API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Token Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            new List<string>()
        }
    });
});

if (string.IsNullOrWhiteSpace(appSettings.JWT_Secret))
{
    throw new InvalidOperationException("JWT Secret is missing");
}

var key = Encoding.UTF8.GetBytes(appSettings.JWT_Secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "AgenticKnowledgeAssistant",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "AgenticKnowledgeAssistant.Client",
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var payload = new JObject
            {
                ["StartTime"] = DateTime.Now,
                ["error"] = context.Error,
                ["error_description"] = context.ErrorDescription,
                ["error_uri"] = context.ErrorUri
            };

            return context.Response.WriteAsync(payload.ToString());
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var userId = context.User?.FindFirst("uid")?.Value
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? Guid.NewGuid().ToString();

        return RateLimitPartition.GetTokenBucketLimiter(userId, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = appSettings.APIRateLimit,
            TokensPerPeriod = appSettings.APIRateLimit,
            ReplenishmentPeriod = TimeSpan.FromSeconds(appSettings.APIRateLimitSeconds),
            AutoReplenishment = true,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, _) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            ReturnCode = 429,
            ReturnMessage = "You have exceeded the allowed number of requests. Please try again later."
        });
    };
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.AllowedHosts = new List<string> { "*" };
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddHttpClient("openai", client =>
{
    client.BaseAddress = new Uri(appSettings.OpenAIEndpoint);
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddServices();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});
builder.Services.AddSignalR();

var app = builder.Build();

try
{
    var connectionString = app.Configuration.GetConnectionString("DefaultConnection") 
        ?? app.Configuration.GetSection("ConnectionStrings")["DefaultConnection"];
    using var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
    conn.Open();
    
    // Ensure table exists
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblAI_UserMfaSettings]') AND type in (N'U'))
        BEGIN
            CREATE TABLE dbo.tblAI_UserMfaSettings (
                UserId INT PRIMARY KEY,
                EmailOtpEnabled BIT NOT NULL,
                SmsOtpEnabled BIT NOT NULL,
                AuthenticatorSecret NVARCHAR(MAX) NULL,
                IsMfaConfigured BIT NOT NULL,
                BackupCodes NVARCHAR(MAX) NULL
            );
        END";
        cmd.ExecuteNonQuery();
    }

    // Ensure usp_AI_GetMfaSettings exists
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_AI_GetMfaSettings]') AND type in (N'P', N'PC'))
        BEGIN
            EXEC('
            CREATE PROCEDURE dbo.usp_AI_GetMfaSettings
                @UserId INT
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT 
                    UserId,
                    EmailOtpEnabled,
                    SmsOtpEnabled,
                    AuthenticatorSecret,
                    IsMfaConfigured,
                    BackupCodes
                FROM tblAI_UserMfaSettings
                WHERE UserId = @UserId;
            END
            ');
        END";
        cmd.ExecuteNonQuery();
    }

    // Ensure usp_AI_SaveMfaSettings exists
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_AI_SaveMfaSettings]') AND type in (N'P', N'PC'))
        BEGIN
            EXEC('
            CREATE PROCEDURE dbo.usp_AI_SaveMfaSettings
                @UserId INT,
                @EmailOtpEnabled BIT,
                @SmsOtpEnabled BIT,
                @AuthenticatorSecret NVARCHAR(MAX) = NULL,
                @IsMfaConfigured BIT,
                @BackupCodes NVARCHAR(MAX) = NULL
            AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (SELECT 1 FROM tblAI_UserMfaSettings WHERE UserId = @UserId)
                BEGIN
                    UPDATE tblAI_UserMfaSettings
                    SET EmailOtpEnabled = @EmailOtpEnabled,
                        SmsOtpEnabled = @SmsOtpEnabled,
                        AuthenticatorSecret = @AuthenticatorSecret,
                        IsMfaConfigured = @IsMfaConfigured,
                        BackupCodes = @BackupCodes
                    WHERE UserId = @UserId;
                END
                ELSE
                BEGIN
                    INSERT INTO tblAI_UserMfaSettings (UserId, EmailOtpEnabled, SmsOtpEnabled, AuthenticatorSecret, IsMfaConfigured, BackupCodes)
                    VALUES (@UserId, @EmailOtpEnabled, @SmsOtpEnabled, @AuthenticatorSecret, @IsMfaConfigured, @BackupCodes);
                END
                SELECT 1;
            END
            ');
        END";
        cmd.ExecuteNonQuery();
    }
}
catch (Exception ex)
{
    Console.WriteLine("MFA SQL Initialization Warning: " + ex.Message);
}

app.UseForwardedHeaders();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<ResponseHeadersMiddleware>();
app.UseMiddleware<BufferedLoggerFlushMiddleware>();
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseCors();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<JwtAuthenticationMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHub<AgenticKnowledgeAssistant.API.Hubs.AssistantHub>("/hubs/assistant");

app.Run();
