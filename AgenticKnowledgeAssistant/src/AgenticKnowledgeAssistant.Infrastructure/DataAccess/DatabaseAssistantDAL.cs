using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using Dapper;
using Microsoft.Data.SqlClient;

namespace AgenticKnowledgeAssistant.DAL;

public sealed class DatabaseAssistantDAL(ConfigurationSettingsListDTO configurationSettings) : IDatabaseAssistantDAL
{
    public async Task<string> GetDefaultDatabaseNameAsync(CancellationToken cancellationToken = default)
    {
        var builder = new SqlConnectionStringBuilder(configurationSettings.DefaultConnection);
        if (!string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            return builder.InitialCatalog;
        }

        await using var connection = new SqlConnection(configurationSettings.DefaultConnection);
        await connection.OpenAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<string>(new CommandDefinition(
            "SELECT DB_NAME();",
            cancellationToken: cancellationToken)) ?? string.Empty;
    }

    public async Task<bool> DatabaseExistsAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(configurationSettings.DefaultConnection);
        await connection.OpenAsync(cancellationToken);

        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM sys.databases WHERE name = @DatabaseName;",
            new { DatabaseName = databaseName },
            cancellationToken: cancellationToken));

        return count > 0;
    }

    public Task<int> CountTablesAsync(string databaseName, ICollection<string> executedSql, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT COUNT(1)
            FROM {QuoteDatabaseName(databaseName)}.INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE';
            """;

        executedSql.Add(sql);
        return ExecuteScalarAsync<int>(sql, cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetTableNamesAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT TOP (@Top) TABLE_NAME
            FROM {QuoteDatabaseName(databaseName)}.INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_NAME;
            """;

        executedSql.Add(sql);
        return QueryStringListAsync(sql, top, cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetStoredProceduresAsync(string databaseName, int top, ICollection<string> executedSql, string nameContains = "", CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT TOP (@Top) name
            FROM {QuoteDatabaseName(databaseName)}.sys.procedures
            WHERE (@NameContains = '' OR name LIKE '%' + @NameContains + '%')
            ORDER BY name;
            """;

        executedSql.Add(sql);
        return QueryStringListAsync(sql, top, cancellationToken, nameContains: nameContains);
    }

    public async Task<string?> GetStoredProcedureDefinitionAsync(string databaseName, string procedureName, ICollection<string> executedSql, CancellationToken cancellationToken = default)
    {
        var quotedDatabaseName = QuoteDatabaseName(databaseName);
        var sql = $"""
            SELECT sm.definition
            FROM {quotedDatabaseName}.sys.sql_modules sm
            INNER JOIN {quotedDatabaseName}.sys.objects o ON sm.object_id = o.object_id
            INNER JOIN {quotedDatabaseName}.sys.schemas s ON o.schema_id = s.schema_id
            WHERE o.type = 'P'
              AND (o.name = @ProcedureName OR CONCAT(s.name, '.', o.name) = @ProcedureName);
            """;

        executedSql.Add(sql);

        await using var connection = new SqlConnection(configurationSettings.DefaultConnection);
        await connection.OpenAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<string?>(new CommandDefinition(
            sql,
            new { ProcedureName = procedureName },
            cancellationToken: cancellationToken));
    }

    public Task<IReadOnlyList<string>> GetViewsAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT TOP (@Top) TABLE_NAME
            FROM {QuoteDatabaseName(databaseName)}.INFORMATION_SCHEMA.VIEWS
            ORDER BY TABLE_NAME;
            """;

        executedSql.Add(sql);
        return QueryStringListAsync(sql, top, cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetFunctionsAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT TOP (@Top) name
            FROM {QuoteDatabaseName(databaseName)}.sys.objects
            WHERE type IN ('FN', 'IF', 'TF', 'FS', 'FT')
            ORDER BY name;
            """;

        executedSql.Add(sql);
        return QueryStringListAsync(sql, top, cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetColumnsAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT TOP (@Top) CONCAT(TABLE_SCHEMA, '.', TABLE_NAME, '.', COLUMN_NAME)
            FROM {QuoteDatabaseName(databaseName)}.INFORMATION_SCHEMA.COLUMNS
            ORDER BY TABLE_SCHEMA, TABLE_NAME, ORDINAL_POSITION;
            """;

        executedSql.Add(sql);
        return QueryStringListAsync(sql, top, cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetIndexesAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT TOP (@Top) CONCAT(s.name, '.', t.name, '.', i.name)
            FROM {QuoteDatabaseName(databaseName)}.sys.indexes i
            INNER JOIN {QuoteDatabaseName(databaseName)}.sys.tables t ON i.object_id = t.object_id
            INNER JOIN {QuoteDatabaseName(databaseName)}.sys.schemas s ON t.schema_id = s.schema_id
            WHERE i.name IS NOT NULL
            ORDER BY s.name, t.name, i.name;
            """;

        executedSql.Add(sql);
        return QueryStringListAsync(sql, top, cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetTriggersAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT TOP (@Top) name
            FROM {QuoteDatabaseName(databaseName)}.sys.triggers
            ORDER BY name;
            """;

        executedSql.Add(sql);
        return QueryStringListAsync(sql, top, cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetForeignKeysAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default)
    {
        var quotedDatabaseName = QuoteDatabaseName(databaseName);
        var sql = $"""
            SELECT TOP (@Top)
                CONCAT(fk.name, ' | ', parent_schema.name, '.', parent_table.name, '.', parent_column.name, ' -> ', referenced_schema.name, '.', referenced_table.name, '.', referenced_column.name)
            FROM {quotedDatabaseName}.sys.foreign_keys fk
            INNER JOIN {quotedDatabaseName}.sys.foreign_key_columns fkc
                ON fk.object_id = fkc.constraint_object_id
            INNER JOIN {quotedDatabaseName}.sys.tables parent_table
                ON fkc.parent_object_id = parent_table.object_id
            INNER JOIN {quotedDatabaseName}.sys.schemas parent_schema
                ON parent_table.schema_id = parent_schema.schema_id
            INNER JOIN {quotedDatabaseName}.sys.columns parent_column
                ON fkc.parent_object_id = parent_column.object_id
                AND fkc.parent_column_id = parent_column.column_id
            INNER JOIN {quotedDatabaseName}.sys.tables referenced_table
                ON fkc.referenced_object_id = referenced_table.object_id
            INNER JOIN {quotedDatabaseName}.sys.schemas referenced_schema
                ON referenced_table.schema_id = referenced_schema.schema_id
            INNER JOIN {quotedDatabaseName}.sys.columns referenced_column
                ON fkc.referenced_object_id = referenced_column.object_id
                AND fkc.referenced_column_id = referenced_column.column_id
            ORDER BY fk.name, fkc.constraint_column_id;
            """;

        executedSql.Add(sql);
        return QueryStringListAsync(sql, top, cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetPrimaryKeysAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default)
    {
        var quotedDatabaseName = QuoteDatabaseName(databaseName);
        var sql = $"""
            SELECT TOP (@Top)
                CONCAT(kc.name, ' | ', s.name, '.', t.name, '.', c.name)
            FROM {quotedDatabaseName}.sys.key_constraints kc
            INNER JOIN {quotedDatabaseName}.sys.tables t ON kc.parent_object_id = t.object_id
            INNER JOIN {quotedDatabaseName}.sys.schemas s ON t.schema_id = s.schema_id
            INNER JOIN {quotedDatabaseName}.sys.index_columns ic ON kc.parent_object_id = ic.object_id AND kc.unique_index_id = ic.index_id
            INNER JOIN {quotedDatabaseName}.sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
            WHERE kc.type = 'PK'
            ORDER BY s.name, t.name, ic.key_ordinal;
            """;

        executedSql.Add(sql);
        return QueryStringListAsync(sql, top, cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetSchemasAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT TOP (@Top) name
            FROM {QuoteDatabaseName(databaseName)}.sys.schemas
            ORDER BY name;
            """;

        executedSql.Add(sql);
        return QueryStringListAsync(sql, top, cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetDatabaseInformationAsync(string databaseName, int top, ICollection<string> executedSql, CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT TOP (@Top)
                CONCAT(name, ' | State: ', state_desc, ' | Recovery: ', recovery_model_desc, ' | Compatibility: ', compatibility_level, ' | Collation: ', collation_name)
            FROM sys.databases
            WHERE name = @DatabaseName
            ORDER BY name;
            """;

        executedSql.Add(sql);
        return QueryStringListAsync(sql, top, cancellationToken, databaseName);
    }

    private async Task<T> ExecuteScalarAsync<T>(string sql, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(configurationSettings.DefaultConnection);
        await connection.OpenAsync(cancellationToken);
        var result = await connection.ExecuteScalarAsync<T>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return result!;
    }

    private async Task<IReadOnlyList<string>> QueryStringListAsync(string sql, int top, CancellationToken cancellationToken, string? databaseName = null, string nameContains = "")
    {
        await using var connection = new SqlConnection(configurationSettings.DefaultConnection);
        await connection.OpenAsync(cancellationToken);

        var rows = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new { Top = Math.Clamp(top, 1, 500), DatabaseName = databaseName, NameContains = nameContains },
            cancellationToken: cancellationToken));

        return rows.ToArray();
    }

    private static string QuoteDatabaseName(string databaseName)
    {
        if (!IsSafeIdentifier(databaseName))
        {
            throw new ArgumentException("Invalid database name.", nameof(databaseName));
        }

        return $"[{databaseName}]";
    }

    private static bool IsSafeIdentifier(string value)
    {
        return value.All(character => char.IsLetterOrDigit(character) || character == '_');
    }
}
