using AgenticKnowledgeAssistant.DAL.Interfaces;
using AgenticKnowledgeAssistant.DTO.Models;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;
using AgenticKnowledgeAssistant.Infrastructure.Persistence;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace AgenticKnowledgeAssistant.DAL;

public sealed class ConversationRepository : IConversationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ConversationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ConversationSessionModel> CreateSessionAsync(int userId, string title, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.tblAI_ConversationSessions (SessionGuid, UserId, Title, Status, CreatedDate, UpdatedDate)
            OUTPUT INSERTED.Id, INSERTED.SessionGuid, INSERTED.UserId, INSERTED.Title, INSERTED.Status, INSERTED.IsPinned, INSERTED.IsFavorite, INSERTED.CreatedDate, INSERTED.UpdatedDate, 0 AS MessageCount, NULL AS LastMessagePreview
            VALUES (NEWID(), @UserId, @Title, N'Active', SYSUTCDATETIME(), SYSUTCDATETIME());
            """;

        return await QuerySingleAsync<ConversationSessionModel>(sql, new { UserId = userId, Title = title }, cancellationToken);
    }

    public async Task<ConversationSessionModel?> GetSessionAsync(Guid sessionGuid, int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (1)
                s.Id,
                s.SessionGuid,
                s.UserId,
                s.Title,
                s.Status,
                s.IsPinned,
                s.IsFavorite,
                s.CreatedDate,
                s.UpdatedDate,
                COUNT(m.MessageId) AS MessageCount,
                MAX(CASE WHEN m.MessageId = lm.LastMessageId THEN LEFT(m.Message, 300) END) AS LastMessagePreview
            FROM dbo.tblAI_ConversationSessions s
            LEFT JOIN dbo.tblAI_ConversationMessages m ON m.SessionId = s.Id
            OUTER APPLY (
                SELECT TOP (1) MessageId AS LastMessageId
                FROM dbo.tblAI_ConversationMessages
                WHERE SessionId = s.Id
                ORDER BY CreatedDate DESC, MessageId DESC
            ) lm
            WHERE s.SessionGuid = @SessionGuid
              AND s.UserId = @UserId
              AND s.Status <> N'Deleted'
            GROUP BY s.Id, s.SessionGuid, s.UserId, s.Title, s.Status, s.IsPinned, s.IsFavorite, s.CreatedDate, s.UpdatedDate;
            """;

        return await QuerySingleOrDefaultAsync<ConversationSessionModel>(sql, new { SessionGuid = sessionGuid, UserId = userId }, cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationSessionModel>> SearchSessionsAsync(int userId, ConversationSearchRequestDTO request, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                s.Id,
                s.SessionGuid,
                s.UserId,
                s.Title,
                s.Status,
                s.IsPinned,
                s.IsFavorite,
                s.CreatedDate,
                s.UpdatedDate,
                COUNT(m.MessageId) AS MessageCount,
                MAX(CASE WHEN m.MessageId = lm.LastMessageId THEN LEFT(m.Message, 300) END) AS LastMessagePreview
            FROM dbo.tblAI_ConversationSessions s
            LEFT JOIN dbo.tblAI_ConversationMessages m ON m.SessionId = s.Id
            OUTER APPLY (
                SELECT TOP (1) MessageId AS LastMessageId
                FROM dbo.tblAI_ConversationMessages
                WHERE SessionId = s.Id
                ORDER BY CreatedDate DESC, MessageId DESC
            ) lm
            WHERE s.UserId = @UserId
              AND s.Status <> N'Deleted'
              AND (@Search IS NULL OR s.Title LIKE '%' + @Search + '%' OR EXISTS (
                    SELECT 1 FROM dbo.tblAI_ConversationMessages sm
                    WHERE sm.SessionId = s.Id AND sm.Message LIKE '%' + @Search + '%'
              ))
              AND (@Pinned IS NULL OR s.IsPinned = @Pinned)
              AND (@Favorite IS NULL OR s.IsFavorite = @Favorite)
            GROUP BY s.Id, s.SessionGuid, s.UserId, s.Title, s.Status, s.IsPinned, s.IsFavorite, s.CreatedDate, s.UpdatedDate
            ORDER BY s.IsPinned DESC, s.UpdatedDate DESC
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
            """;

        var page = Math.Max(1, request.PageNumber);
        var size = Math.Clamp(request.PageSize, 1, 100);
        return await QueryAsync<ConversationSessionModel>(sql, new
        {
            UserId = userId,
            request.Search,
            request.Pinned,
            request.Favorite,
            Skip = (page - 1) * size,
            Take = size
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationMessageModel>> GetMessagesAsync(Guid sessionGuid, int userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                m.MessageId,
                s.SessionGuid,
                s.UserId,
                m.Role,
                m.Message,
                m.Tokens,
                m.CreatedDate,
                m.Metadata
            FROM dbo.tblAI_ConversationMessages m
            INNER JOIN dbo.tblAI_ConversationSessions s ON s.Id = m.SessionId
            WHERE s.SessionGuid = @SessionGuid
              AND s.UserId = @UserId
              AND s.Status <> N'Deleted'
            ORDER BY m.CreatedDate DESC, m.MessageId DESC
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
            """;

        return await QueryAsync<ConversationMessageModel>(sql, new { SessionGuid = sessionGuid, UserId = userId, Skip = skip, Take = take }, cancellationToken);
    }

    public async Task<ConversationMessageModel> SaveMessageAsync(Guid sessionGuid, int userId, string role, string message, int? tokens, string? metadata, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DECLARE @SessionId INT;

            SELECT @SessionId = Id
            FROM dbo.tblAI_ConversationSessions
            WHERE SessionGuid = @SessionGuid AND UserId = @UserId AND Status <> N'Deleted';

            IF @SessionId IS NULL
                THROW 51000, 'Conversation session was not found for this user.', 1;

            INSERT INTO dbo.tblAI_ConversationMessages (SessionId, Role, Message, Tokens, Metadata, CreatedDate)
            OUTPUT INSERTED.MessageId, @SessionGuid AS SessionGuid, @UserId AS UserId, INSERTED.Role, INSERTED.Message, INSERTED.Tokens, INSERTED.CreatedDate, INSERTED.Metadata
            VALUES (@SessionId, @Role, @Message, @Tokens, @Metadata, SYSUTCDATETIME());

            UPDATE dbo.tblAI_ConversationSessions
            SET UpdatedDate = SYSUTCDATETIME(),
                Title = CASE WHEN NULLIF(Title, N'New Chat') IS NULL AND @Role = N'User' THEN LEFT(@Message, 80) ELSE Title END
            WHERE Id = @SessionId;
            """;

        return await QuerySingleAsync<ConversationMessageModel>(sql, new { SessionGuid = sessionGuid, UserId = userId, Role = role, Message = message, Tokens = tokens, Metadata = metadata }, cancellationToken);
    }

    public async Task<bool> UpdateSessionAsync(Guid sessionGuid, int userId, UpdateChatSessionRequestDTO request, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.tblAI_ConversationSessions
            SET Title = COALESCE(NULLIF(@Title, N''), Title),
                IsPinned = COALESCE(@IsPinned, IsPinned),
                IsFavorite = COALESCE(@IsFavorite, IsFavorite),
                Status = COALESCE(NULLIF(@Status, N''), Status),
                UpdatedDate = SYSUTCDATETIME()
            WHERE SessionGuid = @SessionGuid AND UserId = @UserId AND Status <> N'Deleted';
            SELECT @@ROWCOUNT;
            """;

        var rows = await QuerySingleAsync<int>(sql, new { SessionGuid = sessionGuid, UserId = userId, request.Title, request.IsPinned, request.IsFavorite, request.Status }, cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionGuid, int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.tblAI_ConversationSessions
            SET Status = N'Deleted', UpdatedDate = SYSUTCDATETIME()
            WHERE SessionGuid = @SessionGuid AND UserId = @UserId AND Status <> N'Deleted';
            SELECT @@ROWCOUNT;
            """;

        var rows = await QuerySingleAsync<int>(sql, new { SessionGuid = sessionGuid, UserId = userId }, cancellationToken);
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
