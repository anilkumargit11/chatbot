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

DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql += N'
IF COL_LENGTH(N''dbo.' + QUOTENAME(t.name) + N''', N''IsDeleted'') IS NULL
    ALTER TABLE dbo.' + QUOTENAME(t.name) + N' ADD IsDeleted BIT NOT NULL CONSTRAINT DF_' + t.name + N'_IsDeleted_Alter DEFAULT (0);
IF COL_LENGTH(N''dbo.' + QUOTENAME(t.name) + N''', N''IsActive'') IS NULL
    ALTER TABLE dbo.' + QUOTENAME(t.name) + N' ADD IsActive BIT NOT NULL CONSTRAINT DF_' + t.name + N'_IsActive_Alter DEFAULT (1);
IF COL_LENGTH(N''dbo.' + QUOTENAME(t.name) + N''', N''ModifiedDate'') IS NULL
    ALTER TABLE dbo.' + QUOTENAME(t.name) + N' ADD ModifiedDate DATETIME2(3) NULL;
IF COL_LENGTH(N''dbo.' + QUOTENAME(t.name) + N''', N''CreatedDate'') IS NULL
    ALTER TABLE dbo.' + QUOTENAME(t.name) + N' ADD CreatedDate DATETIME2(3) NOT NULL CONSTRAINT DF_' + t.name + N'_CreatedDate_Alter DEFAULT SYSUTCDATETIME();
IF COL_LENGTH(N''dbo.' + QUOTENAME(t.name) + N''', N''CreatedBy'') IS NULL
    ALTER TABLE dbo.' + QUOTENAME(t.name) + N' ADD CreatedBy INT NULL;
IF COL_LENGTH(N''dbo.' + QUOTENAME(t.name) + N''', N''ModifiedBy'') IS NULL
    ALTER TABLE dbo.' + QUOTENAME(t.name) + N' ADD ModifiedBy INT NULL;
'
FROM sys.tables t
WHERE t.schema_id = SCHEMA_ID(N'dbo')
  AND t.name IN
  (
      N'tblAI_Users', N'tblAI_Roles', N'tblAI_UserRoles', N'tblAI_KnowledgeBase', N'tblAI_AgentConfigurations',
      N'tblAI_ChatSessions', N'tblAI_ChatMessages', N'tblAI_Documents', N'tblAI_DocumentChunks', N'tblAI_Embeddings',
      N'tblAI_ApplicationLogs', N'tblAI_ErrorLogs', N'tblAI_AuditLogs'
  );

EXEC sys.sp_executesql @sql;
GO
