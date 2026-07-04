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

DROP INDEX IF EXISTS IX_tblAI_Users_Email ON dbo.tblAI_Users;
GO
CREATE NONCLUSTERED INDEX IX_tblAI_Users_Email
ON dbo.tblAI_Users (Email)
INCLUDE (Id, UserName, DisplayName, IsActive)
WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS IX_tblAI_Users_UserName ON dbo.tblAI_Users;
GO
CREATE NONCLUSTERED INDEX IX_tblAI_Users_UserName
ON dbo.tblAI_Users (UserName)
INCLUDE (Id, Email, DisplayName, IsActive)
WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS IX_tblAI_UserRoles_UserId ON dbo.tblAI_UserRoles;
GO
CREATE NONCLUSTERED INDEX IX_tblAI_UserRoles_UserId
ON dbo.tblAI_UserRoles (UserId, RoleId)
WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS IX_tblAI_UserRoles_RoleId ON dbo.tblAI_UserRoles;
GO
CREATE NONCLUSTERED INDEX IX_tblAI_UserRoles_RoleId
ON dbo.tblAI_UserRoles (RoleId, UserId)
WHERE IsDeleted = 0 AND IsActive = 1;
GO

DROP INDEX IF EXISTS IX_tblAI_Roles_IsActive ON dbo.tblAI_Roles;
GO
CREATE NONCLUSTERED INDEX IX_tblAI_Roles_IsActive
ON dbo.tblAI_Roles (IsActive, RoleName)
INCLUDE (Description, IsSystemRole, CreatedDate)
WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS IX_tblAI_Permissions_IsActive ON dbo.tblAI_Permissions;
GO
CREATE NONCLUSTERED INDEX IX_tblAI_Permissions_IsActive
ON dbo.tblAI_Permissions (IsActive, PermissionName)
INCLUDE (Description)
WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS IX_tblAI_RolePermissions_RoleId ON dbo.tblAI_RolePermissions;
GO
CREATE NONCLUSTERED INDEX IX_tblAI_RolePermissions_RoleId
ON dbo.tblAI_RolePermissions (RoleId, PermissionId)
WHERE IsDeleted = 0 AND IsActive = 1;
GO

DROP INDEX IF EXISTS IX_tblAI_KnowledgeBase_IsActive ON dbo.tblAI_KnowledgeBase;
GO
CREATE NONCLUSTERED INDEX IX_tblAI_KnowledgeBase_IsActive
ON dbo.tblAI_KnowledgeBase (IsActive, CreatedDate DESC)
INCLUDE (Name)
WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS IX_tblAI_AgentConfigurations_IsDefault ON dbo.tblAI_AgentConfigurations;
GO
CREATE NONCLUSTERED INDEX IX_tblAI_AgentConfigurations_IsDefault
ON dbo.tblAI_AgentConfigurations (IsDefault, IsActive)
INCLUDE (AgentName, ModelName, Temperature, MaxTokens)
WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS IX_tblAI_ChatSessions_UserId ON dbo.tblAI_ChatSessions;
GO
CREATE NONCLUSTERED INDEX IX_tblAI_ChatSessions_UserId
ON dbo.tblAI_ChatSessions (UserId, CreatedDate DESC)
INCLUDE (SessionGuid, Title, AgentConfigurationId, IsArchived)
WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS IX_tblAI_ChatMessages_SessionId ON dbo.tblAI_ChatMessages;
GO
CREATE NONCLUSTERED INDEX IX_tblAI_ChatMessages_SessionId
ON dbo.tblAI_ChatMessages (SessionId, CreatedDate ASC)
INCLUDE (UserId, ModelName, PromptTokens, CompletionTokens, LatencyMs)
WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS IX_tblAI_ChatMessages_CreatedDate ON dbo.tblAI_ChatMessages;
GO
CREATE NONCLUSTERED INDEX IX_tblAI_ChatMessages_CreatedDate
ON dbo.tblAI_ChatMessages (CreatedDate DESC)
INCLUDE (SessionId, UserId, ModelName)
WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS IX_tblAI_Documents_KnowledgeBaseId ON dbo.tblAI_Documents;
GO
CREATE NONCLUSTERED INDEX IX_tblAI_Documents_KnowledgeBaseId
ON dbo.tblAI_Documents (KnowledgeBaseId, CreatedDate DESC)
INCLUDE (Title, FileName, FileExtension, ProcessingStatus)
WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS IX_tblAI_DocumentChunks_DocumentId ON dbo.tblAI_DocumentChunks;
GO
CREATE NONCLUSTERED INDEX IX_tblAI_DocumentChunks_DocumentId
ON dbo.tblAI_DocumentChunks (DocumentId, ChunkIndex)
INCLUDE (TokenCount)
WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS IX_tblAI_Embeddings_DocumentId ON dbo.tblAI_Embeddings;
GO
CREATE NONCLUSTERED INDEX IX_tblAI_Embeddings_DocumentId
ON dbo.tblAI_Embeddings (DocumentId)
INCLUDE (DocumentChunkId, ModelName, VectorDimension)
WHERE IsDeleted = 0;
GO

DROP INDEX IF EXISTS IX_tblAI_Embeddings_DocumentChunkId ON dbo.tblAI_Embeddings;
GO
CREATE NONCLUSTERED INDEX IX_tblAI_Embeddings_DocumentChunkId
ON dbo.tblAI_Embeddings (DocumentChunkId)
INCLUDE (DocumentId, ModelName)
WHERE IsDeleted = 0 AND DocumentChunkId IS NOT NULL;
GO

DROP INDEX IF EXISTS IX_tblAI_ApplicationLogs_CreatedDate ON dbo.tblAI_ApplicationLogs;
GO
CREATE NONCLUSTERED INDEX IX_tblAI_ApplicationLogs_CreatedDate
ON dbo.tblAI_ApplicationLogs (CreatedDate DESC, LogLevel)
INCLUDE (Source, TraceId, UserId);
GO

DROP INDEX IF EXISTS IX_tblAI_ErrorLogs_CreatedDate ON dbo.tblAI_ErrorLogs;
GO
CREATE NONCLUSTERED INDEX IX_tblAI_ErrorLogs_CreatedDate
ON dbo.tblAI_ErrorLogs (CreatedDate DESC, IsResolved)
INCLUDE (Source, StatusCode, TraceId, UserId);
GO

DROP INDEX IF EXISTS IX_tblAI_AuditLogs_TableName ON dbo.tblAI_AuditLogs;
GO
CREATE NONCLUSTERED INDEX IX_tblAI_AuditLogs_TableName
ON dbo.tblAI_AuditLogs (TableName, RecordId, CreatedDate DESC)
INCLUDE (ActionType, UserId);
GO

-- Enable after validating SQL Server Full-Text Search is installed:
-- CREATE FULLTEXT CATALOG FTC_Ajay_DB AS DEFAULT;
-- CREATE FULLTEXT INDEX ON dbo.tblAI_Documents(Title LANGUAGE 1033, Content LANGUAGE 1033)
-- KEY INDEX PK_tblAI_Documents WITH CHANGE_TRACKING AUTO;
-- CREATE FULLTEXT INDEX ON dbo.tblAI_DocumentChunks(Content LANGUAGE 1033)
-- KEY INDEX PK_tblAI_DocumentChunks WITH CHANGE_TRACKING AUTO;
-- GO
