SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

USE Ajay_DB;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_LogAudit
    @TableName SYSNAME,
    @RecordId NVARCHAR(100),
    @ActionType NVARCHAR(20),
    @OldValuesJson NVARCHAR(MAX) = NULL,
    @NewValuesJson NVARCHAR(MAX) = NULL,
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tblAI_AuditLogs (TableName, RecordId, ActionType, OldValuesJson, NewValuesJson, UserId)
    VALUES (@TableName, @RecordId, @ActionType, @OldValuesJson, @NewValuesJson, @UserId);

    SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_LogApplication
    @LogLevel NVARCHAR(30),
    @Source NVARCHAR(200) = NULL,
    @Message NVARCHAR(MAX),
    @PropertiesJson NVARCHAR(MAX) = NULL,
    @TraceId NVARCHAR(100) = NULL,
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tblAI_ApplicationLogs (LogLevel, Source, Message, PropertiesJson, TraceId, UserId)
    VALUES (@LogLevel, @Source, @Message, @PropertiesJson, @TraceId, @UserId);

    SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_LogError
    @Source NVARCHAR(200) = NULL,
    @ErrorMessage NVARCHAR(MAX),
    @StackTrace NVARCHAR(MAX) = NULL,
    @RequestPath NVARCHAR(500) = NULL,
    @HttpMethod NVARCHAR(20) = NULL,
    @StatusCode INT = NULL,
    @TraceId NVARCHAR(100) = NULL,
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tblAI_ErrorLogs (Source, ErrorMessage, StackTrace, RequestPath, HttpMethod, StatusCode, TraceId, UserId)
    VALUES (@Source, @ErrorMessage, @StackTrace, @RequestPath, @HttpMethod, @StatusCode, @TraceId, @UserId);

    SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_InsertUser
    @UserName NVARCHAR(100),
    @Email NVARCHAR(256),
    @PasswordHash NVARCHAR(500) = NULL,
    @DisplayName NVARCHAR(200) = NULL,
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tblAI_Users (UserName, Email, PasswordHash, DisplayName, CreatedBy)
    VALUES (@UserName, @Email, @PasswordHash, @DisplayName, @CreatedBy);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_GetUserById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, UserGuid, UserName, Email, DisplayName, IsActive, LastLoginDate, CreatedDate, ModifiedDate
    FROM dbo.tblAI_Users
    WHERE Id = @Id AND IsDeleted = 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_GetUserByEmail
    @Email NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, UserGuid, UserName, Email, PasswordHash, DisplayName, IsActive, LastLoginDate, CreatedDate, ModifiedDate
    FROM dbo.tblAI_Users
    WHERE Email = @Email AND IsDeleted = 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_UpdateUser
    @Id INT,
    @Email NVARCHAR(256),
    @DisplayName NVARCHAR(200) = NULL,
    @IsActive BIT = 1,
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblAI_Users
       SET Email = @Email,
           DisplayName = @DisplayName,
           IsActive = @IsActive,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @Id AND IsDeleted = 0;

    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_DeleteUser
    @Id INT,
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblAI_Users
       SET IsDeleted = 1,
           IsActive = 0,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @Id AND IsDeleted = 0;

    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_InsertRole
    @RoleName NVARCHAR(100),
    @Description NVARCHAR(500) = NULL,
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tblAI_Roles (RoleName, Description, CreatedBy)
    VALUES (@RoleName, @Description, @CreatedBy);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_GetRoles
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, RoleName, Description, IsSystemRole, CreatedDate, ModifiedDate
    FROM dbo.tblAI_Roles
    WHERE IsDeleted = 0
    ORDER BY RoleName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_UpdateRole
    @Id INT,
    @RoleName NVARCHAR(100),
    @Description NVARCHAR(500) = NULL,
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblAI_Roles
       SET RoleName = @RoleName,
           Description = @Description,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @Id AND IsDeleted = 0 AND IsSystemRole = 0;

    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_DeleteRole
    @Id INT,
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblAI_Roles
       SET IsDeleted = 1,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @Id AND IsDeleted = 0 AND IsSystemRole = 0;

    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_InsertUserRole
    @UserId INT,
    @RoleId INT,
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tblAI_UserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
    BEGIN
        UPDATE dbo.tblAI_UserRoles
           SET IsDeleted = 0,
               ModifiedBy = @CreatedBy,
               ModifiedDate = SYSUTCDATETIME()
        WHERE UserId = @UserId AND RoleId = @RoleId;

        SELECT Id FROM dbo.tblAI_UserRoles WHERE UserId = @UserId AND RoleId = @RoleId;
        RETURN;
    END;

    INSERT INTO dbo.tblAI_UserRoles (UserId, RoleId, CreatedBy)
    VALUES (@UserId, @RoleId, @CreatedBy);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_DeleteUserRole
    @UserId INT,
    @RoleId INT,
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblAI_UserRoles
       SET IsDeleted = 1,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE UserId = @UserId AND RoleId = @RoleId AND IsDeleted = 0;

    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_SaveKnowledgeBase
    @Id INT = NULL,
    @Name NVARCHAR(200),
    @Description NVARCHAR(1000) = NULL,
    @OwnerUserId INT = NULL,
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Id IS NULL OR @Id = 0
    BEGIN
        INSERT INTO dbo.tblAI_KnowledgeBase (Name, Description, OwnerUserId, CreatedBy)
        VALUES (@Name, @Description, @OwnerUserId, @UserId);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
        RETURN;
    END;

    UPDATE dbo.tblAI_KnowledgeBase
       SET Name = @Name,
           Description = @Description,
           OwnerUserId = @OwnerUserId,
           ModifiedBy = @UserId,
           ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @Id AND IsDeleted = 0;

    SELECT @Id AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_GetKnowledgeBase
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Description, OwnerUserId, IsActive, CreatedDate, ModifiedDate
    FROM dbo.tblAI_KnowledgeBase
    WHERE IsDeleted = 0
    ORDER BY Name;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_DeleteKnowledgeBase
    @Id INT,
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblAI_KnowledgeBase
       SET IsDeleted = 1,
           IsActive = 0,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @Id AND IsDeleted = 0;

    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_SaveAgentConfiguration
    @Id INT = NULL,
    @AgentName NVARCHAR(150),
    @KnowledgeBaseId INT = NULL,
    @ModelName NVARCHAR(100),
    @SystemPrompt NVARCHAR(MAX) = NULL,
    @Temperature DECIMAL(4,2) = 0.20,
    @MaxTokens INT = 1000,
    @IsDefault BIT = 0,
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    IF @IsDefault = 1
    BEGIN
        UPDATE dbo.tblAI_AgentConfigurations
           SET IsDefault = 0,
               ModifiedDate = SYSUTCDATETIME(),
               ModifiedBy = @UserId
        WHERE IsDeleted = 0;
    END;

    IF @Id IS NULL OR @Id = 0
    BEGIN
        INSERT INTO dbo.tblAI_AgentConfigurations
        (
            AgentName, KnowledgeBaseId, ModelName, SystemPrompt,
            Temperature, MaxTokens, IsDefault, CreatedBy
        )
        VALUES
        (
            @AgentName, @KnowledgeBaseId, @ModelName, @SystemPrompt,
            @Temperature, @MaxTokens, @IsDefault, @UserId
        );

        SET @Id = CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        UPDATE dbo.tblAI_AgentConfigurations
           SET AgentName = @AgentName,
               KnowledgeBaseId = @KnowledgeBaseId,
               ModelName = @ModelName,
               SystemPrompt = @SystemPrompt,
               Temperature = @Temperature,
               MaxTokens = @MaxTokens,
               IsDefault = @IsDefault,
               ModifiedBy = @UserId,
               ModifiedDate = SYSUTCDATETIME()
        WHERE Id = @Id AND IsDeleted = 0;
    END;

    COMMIT TRANSACTION;
    SELECT @Id AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_GetDefaultAgentConfiguration
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        Id, AgentName, KnowledgeBaseId, ModelName, SystemPrompt,
        Temperature, MaxTokens, IsDefault, IsActive, CreatedDate, ModifiedDate
    FROM dbo.tblAI_AgentConfigurations
    WHERE IsDeleted = 0 AND IsActive = 1
    ORDER BY IsDefault DESC, Id ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_GetAgentConfigurations
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id, AgentName, KnowledgeBaseId, ModelName, SystemPrompt,
        Temperature, MaxTokens, IsDefault, IsActive, CreatedDate, ModifiedDate
    FROM dbo.tblAI_AgentConfigurations
    WHERE IsDeleted = 0
    ORDER BY IsDefault DESC, AgentName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_DeleteAgentConfiguration
    @Id INT,
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblAI_AgentConfigurations
       SET IsDeleted = 1,
           IsActive = 0,
           IsDefault = 0,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @Id AND IsDeleted = 0;

    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_InsertChatSession
    @UserId INT = NULL,
    @AgentConfigurationId INT = NULL,
    @Title NVARCHAR(250) = NULL,
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tblAI_ChatSessions (UserId, AgentConfigurationId, Title, CreatedBy)
    VALUES (@UserId, @AgentConfigurationId, @Title, @CreatedBy);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_GetChatSessions
    @UserId INT = NULL,
    @Limit INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Limit)
        Id, SessionGuid, UserId, AgentConfigurationId, Title, IsArchived, CreatedDate, ModifiedDate
    FROM dbo.tblAI_ChatSessions
    WHERE IsDeleted = 0
      AND (@UserId IS NULL OR UserId = @UserId)
    ORDER BY CreatedDate DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_DeleteChatSession
    @SessionId INT,
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    UPDATE dbo.tblAI_ChatMessages
       SET IsDeleted = 1,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE SessionId = @SessionId AND IsDeleted = 0;

    UPDATE dbo.tblAI_ChatSessions
       SET IsDeleted = 1,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @SessionId AND IsDeleted = 0;

    DECLARE @Rows INT = @@ROWCOUNT;
    COMMIT TRANSACTION;

    SELECT @Rows AS RowsAffected;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_SaveChatMessage
    @SessionId INT,
    @UserId INT = NULL,
    @Question NVARCHAR(MAX) = NULL,
    @Response NVARCHAR(MAX) = NULL,
    @PromptTokens INT = NULL,
    @CompletionTokens INT = NULL,
    @ModelName NVARCHAR(100) = NULL,
    @LatencyMs INT = NULL,
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tblAI_ChatMessages
    (
        SessionId, UserId, Question, Response, PromptTokens,
        CompletionTokens, ModelName, LatencyMs, CreatedBy
    )
    VALUES
    (
        @SessionId, @UserId, @Question, @Response, @PromptTokens,
        @CompletionTokens, @ModelName, @LatencyMs, @CreatedBy
    );

    UPDATE dbo.tblAI_ChatSessions
       SET ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @SessionId;

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_GetChatMessages
    @SessionId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, SessionId, UserId, Question, Response, PromptTokens, CompletionTokens, TotalTokens, ModelName, LatencyMs, CreatedDate
    FROM dbo.tblAI_ChatMessages
    WHERE SessionId = @SessionId AND IsDeleted = 0
    ORDER BY CreatedDate ASC, Id ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_UploadDocument
    @Title NVARCHAR(500),
    @Content NVARCHAR(MAX),
    @CreatedDate DATETIME2(3),
    @KnowledgeBaseId INT = NULL,
    @FileName NVARCHAR(260) = NULL,
    @FileExtension NVARCHAR(20) = NULL,
    @ContentType NVARCHAR(150) = NULL,
    @FileSizeBytes BIGINT = NULL,
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @KnowledgeBaseId IS NULL
    BEGIN
        SELECT TOP (1) @KnowledgeBaseId = Id
        FROM dbo.tblAI_KnowledgeBase
        WHERE IsDeleted = 0
        ORDER BY CASE WHEN Name = N'Default' THEN 0 ELSE 1 END, Id;
    END;

    INSERT INTO dbo.tblAI_Documents
    (
        KnowledgeBaseId, Title, FileName, FileExtension, ContentType,
        FileSizeBytes, Content, ContentHash, CreatedDate, CreatedBy
    )
    VALUES
    (
        @KnowledgeBaseId, @Title, @FileName, @FileExtension, @ContentType,
        @FileSizeBytes, @Content, HASHBYTES('SHA2_256', CONVERT(VARBINARY(MAX), @Content)), @CreatedDate, @CreatedBy
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_UpdateDocument
    @Id INT,
    @Title NVARCHAR(500),
    @Content NVARCHAR(MAX),
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblAI_Documents
       SET Title = @Title,
           Content = @Content,
           ContentHash = HASHBYTES('SHA2_256', CONVERT(VARBINARY(MAX), @Content)),
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @Id AND IsDeleted = 0;

    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_GetDocumentById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Title, Content, CreatedDate
    FROM dbo.tblAI_Documents
    WHERE Id = @Id AND IsDeleted = 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_GetDocuments
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Title, Content, CreatedDate
    FROM dbo.tblAI_Documents
    WHERE IsDeleted = 0
    ORDER BY CreatedDate DESC, Id DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_GetDocumentsByIds
    @Ids NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT d.Id, d.Title, d.Content, d.CreatedDate
    FROM dbo.tblAI_Documents d
    INNER JOIN STRING_SPLIT(@Ids, ',') s ON d.Id = TRY_CAST(s.value AS INT)
    WHERE d.IsDeleted = 0
    ORDER BY d.CreatedDate DESC, d.Id DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_SearchDocuments
    @Query NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SearchTerm NVARCHAR(500) = dbo.fn_AI_NormalizeSearchTerm(@Query);

    SELECT TOP (100) Id, Title, Content, CreatedDate
    FROM dbo.tblAI_Documents
    WHERE IsDeleted = 0
      AND
      (
          @SearchTerm IS NULL
          OR Title LIKE N'%' + @SearchTerm + N'%'
          OR Content LIKE N'%' + @SearchTerm + N'%'
      )
    ORDER BY CreatedDate DESC, Id DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_DeleteDocument
    @Id INT,
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    UPDATE dbo.tblAI_Embeddings
       SET IsDeleted = 1, ModifiedBy = @ModifiedBy, ModifiedDate = SYSUTCDATETIME()
    WHERE DocumentId = @Id AND IsDeleted = 0;

    UPDATE dbo.tblAI_DocumentChunks
       SET IsDeleted = 1, ModifiedBy = @ModifiedBy, ModifiedDate = SYSUTCDATETIME()
    WHERE DocumentId = @Id AND IsDeleted = 0;

    UPDATE dbo.tblAI_Documents
       SET IsDeleted = 1, ModifiedBy = @ModifiedBy, ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @Id AND IsDeleted = 0;

    DECLARE @Rows INT = @@ROWCOUNT;

    COMMIT TRANSACTION;
    SELECT @Rows AS RowsAffected;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_UploadDocumentChunk
    @DocumentId INT,
    @ChunkIndex INT,
    @Content NVARCHAR(MAX),
    @TokenCount INT = NULL,
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tblAI_DocumentChunks (DocumentId, ChunkIndex, Content, TokenCount, CreatedBy)
    VALUES (@DocumentId, @ChunkIndex, @Content, @TokenCount, @CreatedBy);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_GetDocumentChunks
    @DocumentId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, DocumentId, ChunkIndex, Content, TokenCount, CreatedDate
    FROM dbo.tblAI_DocumentChunks
    WHERE DocumentId = @DocumentId AND IsDeleted = 0
    ORDER BY ChunkIndex ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_DeleteDocumentChunk
    @Id INT,
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblAI_DocumentChunks
       SET IsDeleted = 1,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @Id AND IsDeleted = 0;

    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_InsertEmbedding
    @DocumentId INT,
    @VectorData NVARCHAR(MAX),
    @DocumentChunkId INT = NULL,
    @ProviderName NVARCHAR(100) = N'OpenAI',
    @ModelName NVARCHAR(100) = NULL,
    @VectorDimension INT = NULL,
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tblAI_Embeddings
    (
        DocumentId, DocumentChunkId, ProviderName, ModelName,
        VectorData, VectorDimension, CreatedBy
    )
    VALUES
    (
        @DocumentId, @DocumentChunkId, @ProviderName, @ModelName,
        @VectorData, @VectorDimension, @CreatedBy
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_GetEmbeddings
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, DocumentId, VectorData
    FROM dbo.tblAI_Embeddings
    WHERE IsDeleted = 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_DeleteEmbedding
    @Id INT,
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblAI_Embeddings
       SET IsDeleted = 1,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @Id AND IsDeleted = 0;

    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_SaveChatHistory
    @Question NVARCHAR(MAX),
    @Response NVARCHAR(MAX),
    @CreatedDate DATETIME2(3),
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    DECLARE @SessionId INT;

    INSERT INTO dbo.tblAI_ChatSessions (UserId, Title, CreatedDate, CreatedBy)
    VALUES (@UserId, LEFT(COALESCE(@Question, N'New chat'), 250), @CreatedDate, @UserId);

    SET @SessionId = CAST(SCOPE_IDENTITY() AS INT);

    INSERT INTO dbo.tblAI_ChatMessages (SessionId, UserId, Question, Response, CreatedDate, CreatedBy)
    VALUES (@SessionId, @UserId, @Question, @Response, @CreatedDate, @UserId);

    DECLARE @MessageId INT = CAST(SCOPE_IDENTITY() AS INT);

    COMMIT TRANSACTION;

    SELECT @MessageId AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_GetChatHistory
    @Limit INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Limit) Id, Question, Response, CreatedDate
    FROM dbo.tblAI_ChatMessages
    WHERE IsDeleted = 0
    ORDER BY CreatedDate DESC, Id DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_GetApplicationLogs
    @FromDate DATETIME2(3) = NULL,
    @ToDate DATETIME2(3) = NULL,
    @LogLevel NVARCHAR(30) = NULL,
    @Limit INT = 200
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Limit)
        Id, LogLevel, Source, Message, PropertiesJson, TraceId, UserId, CreatedDate
    FROM dbo.tblAI_ApplicationLogs
    WHERE (@FromDate IS NULL OR CreatedDate >= @FromDate)
      AND (@ToDate IS NULL OR CreatedDate < @ToDate)
      AND (@LogLevel IS NULL OR LogLevel = @LogLevel)
    ORDER BY CreatedDate DESC, Id DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_GetErrorLogs
    @IsResolved BIT = NULL,
    @Limit INT = 200
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Limit)
        Id, ErrorGuid, Source, ErrorMessage, StackTrace, RequestPath,
        HttpMethod, StatusCode, TraceId, UserId, IsResolved, CreatedDate
    FROM dbo.tblAI_ErrorLogs
    WHERE (@IsResolved IS NULL OR IsResolved = @IsResolved)
    ORDER BY CreatedDate DESC, Id DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_MarkErrorResolved
    @Id BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblAI_ErrorLogs
       SET IsResolved = 1
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO
