using System.Data;
using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.Models;

namespace AgenticKnowledgeAssistant.DAL;

public sealed class DocumentDAL : IDocumentDAL
{
    private readonly ICommonDAL _commonDAL;

    public DocumentDAL(ICommonDAL commonDAL)
    {
        _commonDAL = commonDAL;
    }

    public async Task<int> SaveDocumentDB(DocumentModel documentModel, CancellationToken cancellationToken = default)
    {
        var result = await _commonDAL.ExecuteScalarAsync<int>("dbo.usp_AI_UploadDocument", new[]
        {
            new SqlParameterDefinition { Name = "Title", DbType = SqlDbType.NVarChar, Value = documentModel.Title },
            new SqlParameterDefinition { Name = "Content", DbType = SqlDbType.NVarChar, Value = documentModel.Content },
            new SqlParameterDefinition { Name = "CreatedDate", DbType = SqlDbType.DateTime2, Value = documentModel.CreatedDate }
        }, cancellationToken);

        return result;
    }

    public async Task<DocumentModel?> GetDocumentByIdDB(int id, CancellationToken cancellationToken = default)
    {
        return await _commonDAL.QuerySingleOrDefaultAsync<DocumentModel>("dbo.usp_AI_GetDocumentById", new[]
        {
            new SqlParameterDefinition { Name = "Id", DbType = SqlDbType.Int, Value = id }
        }, cancellationToken);
    }

    public async Task<IEnumerable<DocumentModel>> GetDocumentsByIdsDB(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var csvIds = string.Join(',', ids.Distinct());
        if (string.IsNullOrWhiteSpace(csvIds))
        {
            return Array.Empty<DocumentModel>();
        }

        return await _commonDAL.QueryAsync<DocumentModel>("dbo.usp_AI_GetDocumentsByIds", new[]
        {
            new SqlParameterDefinition { Name = "Ids", DbType = SqlDbType.NVarChar, Value = csvIds }
        }, cancellationToken);
    }

    public async Task<IEnumerable<DocumentModel>> GetDocumentsDB(CancellationToken cancellationToken = default)
    {
        return await _commonDAL.QueryAsync<DocumentModel>("dbo.usp_AI_GetDocuments", Array.Empty<SqlParameterDefinition>(), cancellationToken);
    }

    public async Task<IEnumerable<DocumentModel>> SearchDocumentsDB(string query, CancellationToken cancellationToken = default)
    {
        return await _commonDAL.QueryAsync<DocumentModel>("dbo.usp_AI_SearchDocuments", new[]
        {
            new SqlParameterDefinition { Name = "Query", DbType = SqlDbType.NVarChar, Value = query }
        }, cancellationToken);
    }

    public async Task<bool> DeleteDocumentDB(int id, CancellationToken cancellationToken = default)
    {
        var result = await _commonDAL.ExecuteScalarAsync<int>("dbo.usp_AI_DeleteDocument", new[]
        {
            new SqlParameterDefinition { Name = "Id", DbType = SqlDbType.Int, Value = id }
        }, cancellationToken);

        return result > 0;
    }
}
