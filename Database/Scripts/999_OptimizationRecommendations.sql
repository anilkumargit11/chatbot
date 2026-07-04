/*
    Ajay_DB production optimization recommendations.

    These are intentionally separated from the base deployment because the right
    timing depends on data volume, SQL Server edition, and operational policy.
*/

USE Ajay_DB;
GO

/*
1. Full-text search
   Enable SQL Server Full-Text Search for document retrieval once the feature is
   installed on the SQL Server instance.

   CREATE FULLTEXT CATALOG FTC_Ajay_DB AS DEFAULT;
   CREATE FULLTEXT INDEX ON dbo.tblAI_Documents(Title LANGUAGE 1033, Content LANGUAGE 1033)
   KEY INDEX PK_tblAI_Documents WITH CHANGE_TRACKING AUTO;
   CREATE FULLTEXT INDEX ON dbo.tblAI_DocumentChunks(Content LANGUAGE 1033)
   KEY INDEX PK_tblAI_DocumentChunks WITH CHANGE_TRACKING AUTO;
*/

/*
2. Partitioning
   Partition append-heavy tables monthly by CreatedDate when they become large:
   - dbo.tblAI_ChatMessages
   - dbo.tblAI_ApplicationLogs
   - dbo.tblAI_ErrorLogs
   - dbo.tblAI_AuditLogs

   Recommended trigger point: tens of millions of rows or strict retention/SLA
   requirements requiring sliding-window archive/purge.
*/

/*
3. Retention jobs
   Keep ApplicationLogs and ErrorLogs online for 30-90 days, then archive.
   Keep AuditLogs according to compliance requirements.
*/

/*
4. Security
   Grant the application login EXECUTE on dbo.usp_AI_% procedures only.
   Do not grant direct INSERT/UPDATE/DELETE on base tables to the app login.
*/

/*
5. Query Store
   Query Store is enabled by 000_CreateDatabase.sql. Review top duration and
   regressed queries weekly after production traffic begins.
*/
