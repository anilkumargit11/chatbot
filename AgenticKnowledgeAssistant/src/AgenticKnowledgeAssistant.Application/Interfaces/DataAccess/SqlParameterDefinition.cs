using System.Data;

namespace AgenticKnowledgeAssistant.DAL;

public sealed class SqlParameterDefinition
{
    public string Name { get; set; } = string.Empty;
    public SqlDbType DbType { get; set; }
    public object? Value { get; set; }
}
