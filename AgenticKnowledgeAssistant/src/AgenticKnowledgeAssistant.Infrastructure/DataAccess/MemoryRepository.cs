using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.Models;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;
using AgenticKnowledgeAssistant.Infrastructure.Persistence;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace AgenticKnowledgeAssistant.DAL;

public sealed class MemoryRepository : IMemoryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public MemoryRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserMemoryModel> SaveMemoryAsync(int userId, SaveMemoryRequestDTO request, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.tblAI_UserMemory (UserId, CategoryId, [Key], [Value], IsActive, IsPinned, IsFavorite, CreatedDate, UpdatedDate, Metadata)
            OUTPUT INSERTED.MemoryId, INSERTED.UserId, c.CategoryName AS Category, INSERTED.[Key], INSERTED.[Value], INSERTED.IsActive, INSERTED.IsPinned, INSERTED.IsFavorite, INSERTED.CreatedDate, INSERTED.UpdatedDate, INSERTED.Metadata
            SELECT @UserId, c.CategoryId, @Key, @Value, 1, @IsPinned, @IsFavorite, SYSUTCDATETIME(), SYSUTCDATETIME(), @Metadata
            FROM dbo.tblAI_MemoryCategory c
            WHERE c.CategoryName = @Category;
            """;

        return QuerySingleAsync<UserMemoryModel>(sql, new
        {
            UserId = userId,
            request.Category,
            request.Key,
            request.Value,
            request.IsPinned,
            request.IsFavorite,
            request.Metadata
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<UserMemoryModel>> SearchMemoriesAsync(int userId, MemorySearchRequestDTO request, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                m.MemoryId,
                m.UserId,
                c.CategoryName AS Category,
                m.[Key],
                m.[Value],
                m.IsActive,
                m.IsPinned,
                m.IsFavorite,
                m.CreatedDate,
                m.UpdatedDate,
                m.Metadata
            FROM dbo.tblAI_UserMemory m
            INNER JOIN dbo.tblAI_MemoryCategory c ON c.CategoryId = m.CategoryId
            WHERE m.UserId = @UserId
              AND (@Search IS NULL OR m.[Key] LIKE '%' + @Search + '%' OR m.[Value] LIKE '%' + @Search + '%' OR c.CategoryName LIKE '%' + @Search + '%')
              AND (@Category IS NULL OR c.CategoryName = @Category)
              AND (@IsActive IS NULL OR m.IsActive = @IsActive)
              AND (@IsPinned IS NULL OR m.IsPinned = @IsPinned)
              AND (@IsFavorite IS NULL OR m.IsFavorite = @IsFavorite)
            ORDER BY m.IsPinned DESC, m.IsFavorite DESC, m.UpdatedDate DESC
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
            """;

        var page = Math.Max(1, request.PageNumber);
        var size = Math.Clamp(request.PageSize, 1, 100);
        return await QueryAsync<UserMemoryModel>(sql, new
        {
            UserId = userId,
            request.Search,
            request.Category,
            request.IsActive,
            request.IsPinned,
            request.IsFavorite,
            Skip = (page - 1) * size,
            Take = size
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<MemoryCategoryModel>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT CategoryId, CategoryName, Description
            FROM dbo.tblAI_MemoryCategory
            WHERE IsActive = 1
            ORDER BY SortOrder, CategoryName;
            """;

        return await QueryAsync<MemoryCategoryModel>(sql, new { }, cancellationToken);
    }

    public Task<UserMemoryModel?> GetMemoryAsync(long memoryId, int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (1)
                m.MemoryId,
                m.UserId,
                c.CategoryName AS Category,
                m.[Key],
                m.[Value],
                m.IsActive,
                m.IsPinned,
                m.IsFavorite,
                m.CreatedDate,
                m.UpdatedDate,
                m.Metadata
            FROM dbo.tblAI_UserMemory m
            INNER JOIN dbo.tblAI_MemoryCategory c ON c.CategoryId = m.CategoryId
            WHERE m.MemoryId = @MemoryId AND m.UserId = @UserId;
            """;

        return QuerySingleOrDefaultAsync<UserMemoryModel>(sql, new { MemoryId = memoryId, UserId = userId }, cancellationToken);
    }

    public async Task<bool> UpdateMemoryAsync(long memoryId, int userId, UpdateMemoryRequestDTO request, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DECLARE @CategoryId INT = NULL;

            IF @Category IS NOT NULL
            BEGIN
                SELECT @CategoryId = CategoryId FROM dbo.tblAI_MemoryCategory WHERE CategoryName = @Category;
            END;

            UPDATE dbo.tblAI_UserMemory
            SET CategoryId = COALESCE(@CategoryId, CategoryId),
                [Key] = COALESCE(NULLIF(@Key, N''), [Key]),
                [Value] = COALESCE(NULLIF(@Value, N''), [Value]),
                IsActive = COALESCE(@IsActive, IsActive),
                IsPinned = COALESCE(@IsPinned, IsPinned),
                IsFavorite = COALESCE(@IsFavorite, IsFavorite),
                Metadata = COALESCE(@Metadata, Metadata),
                UpdatedDate = SYSUTCDATETIME()
            WHERE MemoryId = @MemoryId AND UserId = @UserId;

            SELECT @@ROWCOUNT;
            """;

        var rows = await QuerySingleAsync<int>(sql, new
        {
            MemoryId = memoryId,
            UserId = userId,
            request.Category,
            request.Key,
            request.Value,
            request.IsActive,
            request.IsPinned,
            request.IsFavorite,
            request.Metadata
        }, cancellationToken);

        return rows > 0;
    }

    public async Task<bool> DeleteMemoryAsync(long memoryId, int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM dbo.tblAI_UserMemory
            WHERE MemoryId = @MemoryId AND UserId = @UserId;
            SELECT @@ROWCOUNT;
            """;

        var rows = await QuerySingleAsync<int>(sql, new { MemoryId = memoryId, UserId = userId }, cancellationToken);
        return rows > 0;
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
}
