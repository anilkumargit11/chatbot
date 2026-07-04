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

CREATE OR ALTER FUNCTION dbo.fn_AI_NormalizeSearchTerm
(
    @SearchTerm NVARCHAR(500)
)
RETURNS NVARCHAR(500)
AS
BEGIN
    RETURN NULLIF(LTRIM(RTRIM(REPLACE(REPLACE(ISNULL(@SearchTerm, N''), CHAR(13), N' '), CHAR(10), N' '))), N'');
END;
GO

CREATE OR ALTER FUNCTION dbo.fn_AI_ActiveDocuments()
RETURNS TABLE
AS
RETURN
(
    SELECT
        d.Id,
        d.KnowledgeBaseId,
        d.Title,
        d.FileName,
        d.FileExtension,
        d.ContentType,
        d.FileSizeBytes,
        d.Content,
        d.ProcessingStatus,
        d.CreatedDate,
        d.ModifiedDate,
        d.CreatedBy,
        d.ModifiedBy
    FROM dbo.tblAI_Documents d
    WHERE d.IsDeleted = 0
);
GO
