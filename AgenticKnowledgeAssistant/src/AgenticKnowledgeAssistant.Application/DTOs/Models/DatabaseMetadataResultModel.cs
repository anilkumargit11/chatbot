namespace AgenticKnowledgeAssistant.DTO.Models;

public sealed class DatabaseMetadataResultModel
{
    public string DatabaseName { get; set; } = string.Empty;
    public int? TotalTables { get; set; }
    public IReadOnlyList<string> TableNames { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> StoredProcedures { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Views { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Functions { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Triggers { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Indexes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Columns { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> ForeignKeys { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> PrimaryKeys { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Schemas { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> DatabaseInformation { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> ExecutedSql { get; set; } = Array.Empty<string>();
}
