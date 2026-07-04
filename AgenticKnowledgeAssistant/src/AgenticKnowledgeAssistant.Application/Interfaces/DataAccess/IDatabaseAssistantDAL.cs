using AgenticKnowledgeAssistant.DTO.Models;

namespace AgenticKnowledgeAssistant.DAL.Interfaces;

public interface IDatabaseAssistantDAL
{
    Task<string> GetDefaultDatabaseNameAsync(CancellationToken cancellationToken = default);
    Task<bool> DatabaseExistsAsync(string databaseName, CancellationToken cancellationToken = default);
    Task<int> CountTablesAsync(string databaseName, ICollection<string> executedSql, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetTableNamesAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetStoredProceduresAsync(string databaseName, int top, ICollection<string> executedSql, string nameContains = "", CancellationToken cancellationToken = default);
    Task<string?> GetStoredProcedureDefinitionAsync(string databaseName, string procedureName, ICollection<string> executedSql, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetViewsAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetFunctionsAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetColumnsAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetIndexesAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetTriggersAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetForeignKeysAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetPrimaryKeysAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetSchemasAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDatabaseInformationAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default);
}
