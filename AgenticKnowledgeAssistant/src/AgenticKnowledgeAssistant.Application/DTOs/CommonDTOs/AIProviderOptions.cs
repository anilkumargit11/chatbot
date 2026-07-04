namespace AgenticKnowledgeAssistant.DTO.CommonDTOs;

public sealed class AIProviderOptions
{
    public int TimeoutSeconds { get; set; } = 60;
    public bool AutoDetectLocalProviders { get; set; } = true;
    public AzureOpenAIProviderOptions AzureOpenAI { get; set; } = new();
    public OpenAIProviderOptions OpenAI { get; set; } = new();
    public OllamaProviderOptions Ollama { get; set; } = new();
    public LMStudioProviderOptions LMStudio { get; set; } = new();
    public LocalLlamaProviderOptions LocalLlama { get; set; } = new();
    public AzureAIFoundryProviderOptions AzureAIFoundry { get; set; } = new();
}

public sealed class AzureOpenAIProviderOptions
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-02-15-preview";
}

public sealed class OpenAIProviderOptions
{
    public bool Enabled { get; set; } = true;
    public string Endpoint { get; set; } = "https://api.openai.com";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
}

public sealed class OllamaProviderOptions
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.1";
}

public sealed class LMStudioProviderOptions
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = "http://localhost:1234";
    public string Model { get; set; } = "local-model";
}

public sealed class LocalLlamaProviderOptions
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = "http://localhost:8080";
    public string Model { get; set; } = "local-model";
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class AzureAIFoundryProviderOptions
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-05-01-preview";
}
