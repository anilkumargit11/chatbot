SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

/*
    Ajay_DB deployment order for SQL Server 2022.

    Run this file in SQLCMD mode from SQL Server Management Studio, Azure Data Studio,
    or sqlcmd. Paths are relative to this file's folder.
*/

:r .\000_CreateDatabase.sql
:r ..\Tables\001_CreateTables.sql
:r .\010_AlterTables_AuditSoftDelete.sql
:r ..\Constraints\001_Constraints.sql
:r ..\Functions\001_Functions.sql
:r ..\Views\001_Views.sql
:r ..\StoredProcedures\001_AgenticKnowledgeAssistantStoredProcedures.sql
:r ..\StoredProcedures\002_Authentication.sql
:r ..\StoredProcedures\003_Admin_UserRoleManagement.sql
:r ..\Indexes\001_IndexRecommendations.sql
:r .\999_OptimizationRecommendations.sql

USE Ajay_DB;
GO

SELECT
    DB_NAME() AS DatabaseName,
    COUNT(*) AS UserTableCount
FROM sys.tables
WHERE is_ms_shipped = 0;
GO
