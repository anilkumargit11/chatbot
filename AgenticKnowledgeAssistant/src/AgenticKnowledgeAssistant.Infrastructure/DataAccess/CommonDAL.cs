using System.Data;
using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using Dapper;
using Microsoft.Data.SqlClient;

namespace AgenticKnowledgeAssistant.DAL;

public sealed class CommonDAL : ICommonDAL
{
    private readonly ConfigurationSettingsListDTO _configurationSettings;

    public CommonDAL(ConfigurationSettingsListDTO configurationSettings)
    {
        _configurationSettings = configurationSettings;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string procedureName, IEnumerable<SqlParameterDefinition> parameters, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_configurationSettings.DefaultConnection);
        await connection.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            procedureName,
            BuildParameters(parameters),
            commandType: CommandType.StoredProcedure,
            commandTimeout: 60,
            cancellationToken: cancellationToken);

        return await connection.QueryAsync<T>(command);
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string procedureName, IEnumerable<SqlParameterDefinition> parameters, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_configurationSettings.DefaultConnection);
        await connection.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            procedureName,
            BuildParameters(parameters),
            commandType: CommandType.StoredProcedure,
            commandTimeout: 60,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<T>(command);
    }

    public async Task<T?> ExecuteScalarAsync<T>(string procedureName, IEnumerable<SqlParameterDefinition> parameters, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_configurationSettings.DefaultConnection);
        await connection.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            procedureName,
            BuildParameters(parameters),
            commandType: CommandType.StoredProcedure,
            commandTimeout: 60,
            cancellationToken: cancellationToken);

        return await connection.ExecuteScalarAsync<T>(command);
    }

    private static DynamicParameters BuildParameters(IEnumerable<SqlParameterDefinition> parameters)
    {
        var dynamicParameters = new DynamicParameters();

        foreach (var parameter in parameters)
        {
            dynamicParameters.Add(
                parameter.Name,
                parameter.Value,
                ConvertToDbType(parameter.DbType));
        }

        return dynamicParameters;
    }

    private static DbType ConvertToDbType(SqlDbType sqlDbType)
    {
        return sqlDbType switch
        {
            SqlDbType.BigInt => DbType.Int64,
            SqlDbType.Bit => DbType.Boolean,
            SqlDbType.DateTime => DbType.DateTime,
            SqlDbType.DateTime2 => DbType.DateTime2,
            SqlDbType.Decimal => DbType.Decimal,
            SqlDbType.Float => DbType.Double,
            SqlDbType.Int => DbType.Int32,
            SqlDbType.NVarChar => DbType.String,
            SqlDbType.UniqueIdentifier => DbType.Guid,
            SqlDbType.VarBinary => DbType.Binary,
            _ => DbType.Object
        };
    }
}
