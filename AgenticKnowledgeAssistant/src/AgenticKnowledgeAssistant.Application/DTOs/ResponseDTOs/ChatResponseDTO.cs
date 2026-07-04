using System.Text.Json.Serialization;

namespace AgenticKnowledgeAssistant.DTO.ResponseDTOs;

public sealed class ChatResponseDTO
{
    public string Answer { get; set; } = string.Empty;
    public Guid? SessionGuid { get; set; }
    public IEnumerable<string> Sources { get; set; } = Array.Empty<string>();
    public string ToolUsed { get; set; } = string.Empty;
    public object? StructuredData { get; set; }
    public double? ConfidenceScore { get; set; }
    public long? ResponseTimeMs { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public int? TotalTokens { get; set; }
    public string? DetectedLanguage { get; set; }
    public string? TranslatedAnswer { get; set; }
}

public sealed class DatabaseAssistantResultDTO
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

public sealed class DatabaseUserDTO
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }
    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }
}

public sealed class RegisteredUserCountDTO
{
    [JsonPropertyName("registeredUsers")]
    public int RegisteredUsers { get; set; }
}

public sealed class DocumentMetadataResponseDTO
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    [JsonPropertyName("documentType")]
    public string DocumentType { get; set; } = string.Empty;
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
    [JsonPropertyName("documents")]
    public IReadOnlyList<DocumentMetadataItemDTO> Documents { get; set; } = Array.Empty<DocumentMetadataItemDTO>();
}

public sealed class DocumentMetadataItemDTO
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("documentName")]
    public string DocumentName { get; set; } = string.Empty;
    [JsonPropertyName("fileType")]
    public string FileType { get; set; } = string.Empty;
    [JsonPropertyName("uploadDate")]
    public DateTime UploadDate { get; set; }
}

public sealed class DatabaseMetadataResponseDTO
{
    [JsonPropertyName("databaseName")]
    public string DatabaseName { get; set; } = string.Empty;
    [JsonPropertyName("totalTables")]
    public int? TotalTables { get; set; }
    [JsonPropertyName("topTables")]
    public IReadOnlyList<string> TopTables { get; set; } = Array.Empty<string>();
    [JsonPropertyName("tables")]
    public IReadOnlyList<string> Tables { get; set; } = Array.Empty<string>();
    [JsonPropertyName("storedProcedures")]
    public IReadOnlyList<string> StoredProcedures { get; set; } = Array.Empty<string>();
    [JsonPropertyName("views")]
    public IReadOnlyList<string> Views { get; set; } = Array.Empty<string>();
    [JsonPropertyName("functions")]
    public IReadOnlyList<string> Functions { get; set; } = Array.Empty<string>();
    [JsonPropertyName("columns")]
    public IReadOnlyList<string> Columns { get; set; } = Array.Empty<string>();
    [JsonPropertyName("indexes")]
    public IReadOnlyList<string> Indexes { get; set; } = Array.Empty<string>();
    [JsonPropertyName("triggers")]
    public IReadOnlyList<string> Triggers { get; set; } = Array.Empty<string>();
    [JsonPropertyName("foreignKeys")]
    public IReadOnlyList<string> ForeignKeys { get; set; } = Array.Empty<string>();
    [JsonPropertyName("primaryKeys")]
    public IReadOnlyList<string> PrimaryKeys { get; set; } = Array.Empty<string>();
    [JsonPropertyName("schemas")]
    public IReadOnlyList<string> Schemas { get; set; } = Array.Empty<string>();
    [JsonPropertyName("databaseInformation")]
    public IReadOnlyList<string> DatabaseInformation { get; set; } = Array.Empty<string>();
    [JsonPropertyName("executedSql")]
    public IReadOnlyList<string> ExecutedSql { get; set; } = Array.Empty<string>();
}
