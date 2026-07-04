using System.Data;
using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.Models;

namespace AgenticKnowledgeAssistant.DAL;

public sealed class AgentDAL : IAgentDAL
{
    private readonly ICommonDAL _commonDAL;

    public AgentDAL(ICommonDAL commonDAL)
    {
        _commonDAL = commonDAL;
    }

    public async Task<int> SaveEmbeddingDB(EmbeddingModel embeddingModel, CancellationToken cancellationToken = default)
    {
        var result = await _commonDAL.ExecuteScalarAsync<int>("dbo.usp_AI_InsertEmbedding", new[]
        {
            new SqlParameterDefinition { Name = "DocumentId", DbType = SqlDbType.Int, Value = embeddingModel.DocumentId },
            new SqlParameterDefinition { Name = "VectorData", DbType = SqlDbType.NVarChar, Value = embeddingModel.VectorData }
        }, cancellationToken);

        return result;
    }

    public async Task<IEnumerable<EmbeddingModel>> GetEmbeddingsDB(CancellationToken cancellationToken = default)
    {
        return await _commonDAL.QueryAsync<EmbeddingModel>("dbo.usp_AI_GetEmbeddings", Array.Empty<SqlParameterDefinition>(), cancellationToken);
    }
}
