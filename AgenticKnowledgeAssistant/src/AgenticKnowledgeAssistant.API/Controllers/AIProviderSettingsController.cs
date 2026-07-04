using System.Text.Json;
using AgenticKnowledgeAssistant.BAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AgenticKnowledgeAssistant.API.Controllers;

[ApiController]
[Authorize]
[Route("api/ai-providers")]
public sealed class AIProviderSettingsController : ControllerBase
{
    private const string LocalSettingsFileName = "ai-provider-settings.local.json";
    private readonly IOptionsMonitor<AIProviderOptions> _options;
    private readonly IAIProviderHealthService _providerHealthService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AIProviderSettingsController> _logger;

    public AIProviderSettingsController(
        IOptionsMonitor<AIProviderOptions> options,
        IAIProviderHealthService providerHealthService,
        IWebHostEnvironment environment,
        ILogger<AIProviderSettingsController> logger)
    {
        _options = options;
        _providerHealthService = providerHealthService;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            data = new
            {
                settings = MaskSecrets(_options.CurrentValue),
                providers = _providerHealthService.GetConfigurationStatus()
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> Save(AIProviderOptions settings, CancellationToken cancellationToken)
    {
        settings.AzureOpenAI.ApiKey = PreserveMaskedSecret(settings.AzureOpenAI.ApiKey, _options.CurrentValue.AzureOpenAI.ApiKey);
        settings.OpenAI.ApiKey = PreserveMaskedSecret(settings.OpenAI.ApiKey, _options.CurrentValue.OpenAI.ApiKey);
        settings.LocalLlama.ApiKey = PreserveMaskedSecret(settings.LocalLlama.ApiKey, _options.CurrentValue.LocalLlama.ApiKey);
        settings.AzureAIFoundry.ApiKey = PreserveMaskedSecret(settings.AzureAIFoundry.ApiKey, _options.CurrentValue.AzureAIFoundry.ApiKey);

        var filePath = Path.Combine(_environment.ContentRootPath, LocalSettingsFileName);
        var payload = new { AIProviders = settings };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

        await System.IO.File.WriteAllTextAsync(filePath, json, cancellationToken);
        _logger.LogInformation("AI provider settings saved to local override file.");

        return Ok(new { data = new { message = "AI provider settings saved." } });
    }

    [HttpPost("test")]
    public async Task<IActionResult> Test(CancellationToken cancellationToken)
    {
        var results = await _providerHealthService.CheckHealthAsync(cancellationToken);
        return Ok(new { data = results });
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
        => Ok(new { data = await _providerHealthService.CheckHealthAsync(cancellationToken) });

    private static AIProviderOptions MaskSecrets(AIProviderOptions source)
    {
        return new AIProviderOptions
        {
            TimeoutSeconds = source.TimeoutSeconds,
            AutoDetectLocalProviders = source.AutoDetectLocalProviders,
            AzureOpenAI = new AzureOpenAIProviderOptions
            {
                Enabled = source.AzureOpenAI.Enabled,
                Endpoint = source.AzureOpenAI.Endpoint,
                DeploymentName = source.AzureOpenAI.DeploymentName,
                ApiVersion = source.AzureOpenAI.ApiVersion,
                ApiKey = Mask(source.AzureOpenAI.ApiKey)
            },
            OpenAI = new OpenAIProviderOptions
            {
                Enabled = source.OpenAI.Enabled,
                Endpoint = source.OpenAI.Endpoint,
                Model = source.OpenAI.Model,
                ApiKey = Mask(source.OpenAI.ApiKey)
            },
            Ollama = source.Ollama,
            LMStudio = source.LMStudio,
            LocalLlama = new LocalLlamaProviderOptions
            {
                Enabled = source.LocalLlama.Enabled,
                Endpoint = source.LocalLlama.Endpoint,
                Model = source.LocalLlama.Model,
                ApiKey = Mask(source.LocalLlama.ApiKey)
            },
            AzureAIFoundry = new AzureAIFoundryProviderOptions
            {
                Enabled = source.AzureAIFoundry.Enabled,
                Endpoint = source.AzureAIFoundry.Endpoint,
                DeploymentName = source.AzureAIFoundry.DeploymentName,
                ApiVersion = source.AzureAIFoundry.ApiVersion,
                ApiKey = Mask(source.AzureAIFoundry.ApiKey)
            }
        };
    }

    private static string Mask(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Length <= 8 ? "********" : $"{value[..4]}****{value[^4..]}";
    }

    private static string PreserveMaskedSecret(string submitted, string existing)
    {
        return submitted.Contains('*', StringComparison.Ordinal) ? existing : submitted;
    }
}
