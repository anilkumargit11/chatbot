USE Ajay_DB;
GO

DECLARE @ProcedureName SYSNAME;
DECLARE procedure_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT QUOTENAME(SCHEMA_NAME(schema_id)) + N'.' + QUOTENAME(name)
FROM sys.procedures
WHERE name LIKE N'usp_AI_%';

OPEN procedure_cursor;
FETCH NEXT FROM procedure_cursor INTO @ProcedureName;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC (N'DROP PROCEDURE ' + @ProcedureName);
    FETCH NEXT FROM procedure_cursor INTO @ProcedureName;
END;
CLOSE procedure_cursor;
DEALLOCATE procedure_cursor;
GO

DROP VIEW IF EXISTS dbo.vw_AI_ActiveUsers;
DROP VIEW IF EXISTS dbo.vw_AI_ChatHistory;
DROP VIEW IF EXISTS dbo.vw_AI_DocumentSummary;
GO

DROP FUNCTION IF EXISTS dbo.fn_AI_ActiveDocuments;
DROP FUNCTION IF EXISTS dbo.fn_AI_NormalizeSearchTerm;
GO

DROP TABLE IF EXISTS dbo.tblAI_AuditLogs;
DROP TABLE IF EXISTS dbo.tblAI_ErrorLogs;
DROP TABLE IF EXISTS dbo.tblAI_ApplicationLogs;
DROP TABLE IF EXISTS dbo.tblAI_Embeddings;
DROP TABLE IF EXISTS dbo.tblAI_DocumentChunks;
DROP TABLE IF EXISTS dbo.tblAI_Documents;
DROP TABLE IF EXISTS dbo.tblAI_ChatMessages;
DROP TABLE IF EXISTS dbo.tblAI_ChatSessions;
DROP TABLE IF EXISTS dbo.tblAI_AgentConfigurations;
DROP TABLE IF EXISTS dbo.tblAI_KnowledgeBase;
DROP TABLE IF EXISTS dbo.tblAI_UserRoles;
DROP TABLE IF EXISTS dbo.tblAI_Roles;
DROP TABLE IF EXISTS dbo.tblAI_Users;
GO
