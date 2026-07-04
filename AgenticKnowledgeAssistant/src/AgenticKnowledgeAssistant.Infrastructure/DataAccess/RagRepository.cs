using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.Models;
using AgenticKnowledgeAssistant.Infrastructure.Persistence;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace AgenticKnowledgeAssistant.DAL;

public sealed class RagRepository : IRagRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RagRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<long> CreateDocumentAsync(RagDocumentModel document, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.tblAI_RagDocuments (UserId, FileName, ContentType, FileSizeBytes, Title, ProcessingStatus, Summary, Metadata, CreatedDate, UpdatedDate)
            OUTPUT INSERTED.DocumentId
            VALUES (@UserId, @FileName, @ContentType, @FileSizeBytes, @Title, @ProcessingStatus, @Summary, @Metadata, SYSUTCDATETIME(), SYSUTCDATETIME());
            """;

        return QuerySingleAsync<long>(sql, document, cancellationToken);
    }

    public Task UpdateDocumentStatusAsync(long documentId, int userId, string status, int chunkCount, int embeddingCount, string? summary, string? metadata, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.tblAI_RagDocuments
            SET ProcessingStatus = @Status,
                ChunkCount = @ChunkCount,
                EmbeddingCount = @EmbeddingCount,
                Summary = COALESCE(@Summary, Summary),
                Metadata = COALESCE(@Metadata, Metadata),
                UpdatedDate = SYSUTCDATETIME()
            WHERE DocumentId = @DocumentId AND UserId = @UserId;
            """;

        return ExecuteAsync(sql, new { DocumentId = documentId, UserId = userId, Status = status, ChunkCount = chunkCount, EmbeddingCount = embeddingCount, Summary = summary, Metadata = metadata }, cancellationToken);
    }

    public Task<RagDocumentModel?> GetDocumentAsync(long documentId, int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT DocumentId, UserId, FileName, ContentType, FileSizeBytes, Title, ProcessingStatus, ChunkCount, EmbeddingCount, Summary, Metadata, CreatedDate, UpdatedDate
            FROM dbo.tblAI_RagDocuments
            WHERE DocumentId = @DocumentId AND UserId = @UserId AND IsDeleted = 0;
            """;

        return QuerySingleOrDefaultAsync<RagDocumentModel>(sql, new { DocumentId = documentId, UserId = userId }, cancellationToken);
    }

    public async Task<IReadOnlyList<RagDocumentModel>> GetDocumentsAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT DocumentId, UserId, FileName, ContentType, FileSizeBytes, Title, ProcessingStatus, ChunkCount, EmbeddingCount, Summary, Metadata, CreatedDate, UpdatedDate
            FROM dbo.tblAI_RagDocuments
            WHERE UserId = @UserId AND IsDeleted = 0
            ORDER BY UpdatedDate DESC;
            """;

        return await QueryAsync<RagDocumentModel>(sql, new { UserId = userId }, cancellationToken);
    }

    public async Task<bool> DeleteDocumentAsync(long documentId, int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.tblAI_RagDocuments
            SET IsDeleted = 1, ProcessingStatus = N'Deleted', UpdatedDate = SYSUTCDATETIME()
            WHERE DocumentId = @DocumentId AND UserId = @UserId AND IsDeleted = 0;
            SELECT @@ROWCOUNT;
            """;

        var rows = await QuerySingleAsync<int>(sql, new { DocumentId = documentId, UserId = userId }, cancellationToken);
        return rows > 0;
    }

    public Task DeleteChunksAsync(long documentId, int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE e
            FROM dbo.tblAI_RagEmbeddings e
            INNER JOIN dbo.tblAI_RagChunks c ON c.ChunkId = e.ChunkId
            INNER JOIN dbo.tblAI_RagDocuments d ON d.DocumentId = c.DocumentId
            WHERE d.DocumentId = @DocumentId AND d.UserId = @UserId;

            DELETE c
            FROM dbo.tblAI_RagChunks c
            INNER JOIN dbo.tblAI_RagDocuments d ON d.DocumentId = c.DocumentId
            WHERE d.DocumentId = @DocumentId AND d.UserId = @UserId;
            """;

        return ExecuteAsync(sql, new { DocumentId = documentId, UserId = userId }, cancellationToken);
    }

    public Task<long> SaveChunkAsync(long documentId, int userId, RagChunkModel chunk, string? embeddingJson, string provider, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DECLARE @ChunkId BIGINT;

            INSERT INTO dbo.tblAI_RagChunks (DocumentId, ChunkIndex, PageNumber, Section, Heading, Content, TokenCount, Metadata, CreatedDate)
            VALUES (@DocumentId, @ChunkIndex, @PageNumber, @Section, @Heading, @Content, @TokenCount, @Metadata, SYSUTCDATETIME());

            SET @ChunkId = SCOPE_IDENTITY();

            IF @EmbeddingJson IS NOT NULL
            BEGIN
                INSERT INTO dbo.tblAI_RagEmbeddings (ChunkId, ProviderName, ModelName, VectorData, VectorDimension, CreatedDate)
                VALUES (@ChunkId, @Provider, N'text-embedding-3-small', @EmbeddingJson, 1536, SYSUTCDATETIME());
            END;

            SELECT @ChunkId;
            """;

        return QuerySingleAsync<long>(sql, new
        {
            DocumentId = documentId,
            UserId = userId,
            chunk.ChunkIndex,
            chunk.PageNumber,
            chunk.Section,
            chunk.Heading,
            chunk.Content,
            chunk.TokenCount,
            chunk.Metadata,
            EmbeddingJson = embeddingJson,
            Provider = provider
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<RagSearchResultModel>> KeywordSearchAsync(int userId, string query, IReadOnlyList<long> documentIds, int topK, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (@TopK)
                d.DocumentId,
                c.ChunkId,
                d.FileName,
                d.Title,
                c.ChunkIndex,
                c.PageNumber,
                c.Section,
                c.Heading,
                c.Content,
                CAST((
                    SELECT COUNT(1)
                    FROM STRING_SPLIT(@Query, N' ') token
                    WHERE LEN(token.value) >= 3 AND c.Content LIKE N'%' + token.value + N'%'
                ) AS FLOAT) AS KeywordScore,
                CAST(0 AS FLOAT) AS VectorScore,
                CAST(0 AS FLOAT) AS HybridScore
            FROM dbo.tblAI_RagChunks c
            INNER JOIN dbo.tblAI_RagDocuments d ON d.DocumentId = c.DocumentId
            WHERE d.UserId = @UserId
              AND d.IsDeleted = 0
              AND (@DocumentIds = N'' OR d.DocumentId IN (SELECT TRY_CONVERT(BIGINT, value) FROM STRING_SPLIT(@DocumentIds, N',')))
              AND EXISTS (
                    SELECT 1 FROM STRING_SPLIT(@Query, N' ') token
                    WHERE LEN(token.value) >= 3 AND c.Content LIKE N'%' + token.value + N'%'
              )
            ORDER BY KeywordScore DESC, d.UpdatedDate DESC;
            """;

        return await QueryAsync<RagSearchResultModel>(sql, new
        {
            UserId = userId,
            Query = query,
            DocumentIds = string.Join(',', documentIds.Distinct()),
            TopK = topK
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<(RagSearchResultModel Chunk, string VectorData)>> GetSearchableEmbeddingsAsync(int userId, IReadOnlyList<long> documentIds, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                d.DocumentId,
                c.ChunkId,
                d.FileName,
                d.Title,
                c.ChunkIndex,
                c.PageNumber,
                c.Section,
                c.Heading,
                c.Content,
                CAST(0 AS FLOAT) AS KeywordScore,
                CAST(0 AS FLOAT) AS VectorScore,
                CAST(0 AS FLOAT) AS HybridScore,
                e.VectorData
            FROM dbo.tblAI_RagEmbeddings e
            INNER JOIN dbo.tblAI_RagChunks c ON c.ChunkId = e.ChunkId
            INNER JOIN dbo.tblAI_RagDocuments d ON d.DocumentId = c.DocumentId
            WHERE d.UserId = @UserId
              AND d.IsDeleted = 0
              AND (@DocumentIds = N'' OR d.DocumentId IN (SELECT TRY_CONVERT(BIGINT, value) FROM STRING_SPLIT(@DocumentIds, N',')));
            """;

        var rows = await QueryAsync<RagEmbeddingRow>(sql, new { UserId = userId, DocumentIds = string.Join(',', documentIds.Distinct()) }, cancellationToken);
        return rows.Select(row => (new RagSearchResultModel
        {
            DocumentId = row.DocumentId,
            ChunkId = row.ChunkId,
            FileName = row.FileName,
            Title = row.Title,
            ChunkIndex = row.ChunkIndex,
            PageNumber = row.PageNumber,
            Section = row.Section,
            Heading = row.Heading,
            Content = row.Content,
            KeywordScore = row.KeywordScore,
            VectorScore = row.VectorScore,
            HybridScore = row.HybridScore
        }, row.VectorData)).ToArray();
    }

    public Task SaveSearchHistoryAsync(int userId, string query, int resultCount, string searchType, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.tblAI_RagSearchHistory (UserId, Query, ResultCount, SearchType, CreatedDate)
            VALUES (@UserId, @Query, @ResultCount, @SearchType, SYSUTCDATETIME());
            """;

        return ExecuteAsync(sql, new { UserId = userId, Query = query, ResultCount = resultCount, SearchType = searchType }, cancellationToken);
    }

    private async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object parameters, CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var rows = await _dbContext.Database.GetDbConnection().QueryAsync<T>(command);
        return rows.AsList();
    }

    private async Task<T> QuerySingleAsync<T>(string sql, object parameters, CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return await _dbContext.Database.GetDbConnection().QuerySingleAsync<T>(command);
    }

    private async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object parameters, CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        return await _dbContext.Database.GetDbConnection().QuerySingleOrDefaultAsync<T>(command);
    }

    private async Task ExecuteAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        await _dbContext.Database.GetDbConnection().ExecuteAsync(command);
    }

    private sealed class RagEmbeddingRow
    {
        public long DocumentId { get; set; }
        public long ChunkId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public int? PageNumber { get; set; }
        public string Section { get; set; } = string.Empty;
        public string Heading { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public double KeywordScore { get; set; }
        public double VectorScore { get; set; }
        public double HybridScore { get; set; }
        public string VectorData { get; set; } = string.Empty;
    }
}
