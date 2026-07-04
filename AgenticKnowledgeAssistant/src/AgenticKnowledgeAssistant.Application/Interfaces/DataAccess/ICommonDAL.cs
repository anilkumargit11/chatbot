namespace AgenticKnowledgeAssistant.DAL.Interfaces;

public interface ICommonDAL
{
    Task<IEnumerable<T>> QueryAsync<T>(string procedureName, IEnumerable<SqlParameterDefinition> parameters, CancellationToken cancellationToken = default);
    Task<T?> QuerySingleOrDefaultAsync<T>(string procedureName, IEnumerable<SqlParameterDefinition> parameters, CancellationToken cancellationToken = default);
    Task<T?> ExecuteScalarAsync<T>(string procedureName, IEnumerable<SqlParameterDefinition> parameters, CancellationToken cancellationToken = default);
}
