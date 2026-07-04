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

CREATE OR ALTER VIEW dbo.vw_AI_DocumentSummary
AS
    SELECT
        d.Id,
        d.KnowledgeBaseId,
        kb.Name AS KnowledgeBaseName,
        d.Title,
        d.FileName,
        d.FileExtension,
        d.FileSizeBytes,
        d.ProcessingStatus,
        d.CreatedDate,
        d.ModifiedDate,
        COUNT(dc.Id) AS ChunkCount,
        COUNT(e.Id) AS EmbeddingCount
    FROM dbo.tblAI_Documents d
    LEFT JOIN dbo.tblAI_KnowledgeBase kb ON kb.Id = d.KnowledgeBaseId
    LEFT JOIN dbo.tblAI_DocumentChunks dc ON dc.DocumentId = d.Id AND dc.IsDeleted = 0
    LEFT JOIN dbo.tblAI_Embeddings e ON e.DocumentId = d.Id AND e.IsDeleted = 0
    WHERE d.IsDeleted = 0
    GROUP BY
        d.Id, d.KnowledgeBaseId, kb.Name, d.Title, d.FileName, d.FileExtension,
        d.FileSizeBytes, d.ProcessingStatus, d.CreatedDate, d.ModifiedDate;
GO

CREATE OR ALTER VIEW dbo.vw_AI_ChatHistory
AS
    SELECT
        cm.Id,
        cm.SessionId,
        cs.SessionGuid,
        cs.Title,
        cm.UserId,
        cm.Question,
        cm.Response,
        cm.ModelName,
        cm.PromptTokens,
        cm.CompletionTokens,
        cm.TotalTokens,
        cm.LatencyMs,
        cm.CreatedDate
    FROM dbo.tblAI_ChatMessages cm
    INNER JOIN dbo.tblAI_ChatSessions cs ON cs.Id = cm.SessionId
    WHERE cm.IsDeleted = 0
      AND cs.IsDeleted = 0;
GO

CREATE OR ALTER VIEW dbo.vw_AI_ActiveUsers
AS
    SELECT
        u.Id,
        u.UserGuid,
        u.UserName,
        u.Email,
        u.DisplayName,
        u.IsActive,
        u.LastLoginDate,
        u.CreatedDate,
        STRING_AGG(r.RoleName, N',') AS Roles
    FROM dbo.tblAI_Users u
    LEFT JOIN dbo.tblAI_UserRoles ur ON ur.UserId = u.Id AND ur.IsDeleted = 0
    LEFT JOIN dbo.tblAI_Roles r ON r.Id = ur.RoleId AND r.IsDeleted = 0
    WHERE u.IsDeleted = 0
    GROUP BY u.Id, u.UserGuid, u.UserName, u.Email, u.DisplayName, u.IsActive, u.LastLoginDate, u.CreatedDate;
GO
