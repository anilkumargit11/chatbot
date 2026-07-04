using System.Data;
using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.Models;

namespace AgenticKnowledgeAssistant.DAL;

public sealed class ChatDAL : IChatDAL
{
    private readonly ICommonDAL _commonDAL;

    public ChatDAL(ICommonDAL commonDAL)
    {
        _commonDAL = commonDAL;
    }

    public async Task<int> SaveChatHistoryDB(ChatHistoryModel chatHistoryModel, CancellationToken cancellationToken = default)
    {
        var result = await _commonDAL.ExecuteScalarAsync<int>("dbo.usp_AI_SaveChatHistory", new[]
        {
            new SqlParameterDefinition { Name = "Question", DbType = SqlDbType.NVarChar, Value = chatHistoryModel.Question },
            new SqlParameterDefinition { Name = "Response", DbType = SqlDbType.NVarChar, Value = chatHistoryModel.Response },
            new SqlParameterDefinition { Name = "CreatedDate", DbType = SqlDbType.DateTime2, Value = chatHistoryModel.CreatedDate }
        }, cancellationToken);

        return result;
    }

    public async Task<IEnumerable<ChatHistoryModel>> GetRecentChatHistoryDB(int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _commonDAL.QueryAsync<ChatHistoryModel>("dbo.usp_AI_GetChatHistory", new[]
        {
            new SqlParameterDefinition { Name = "Limit", DbType = SqlDbType.Int, Value = limit }
        }, cancellationToken);
    }
}
