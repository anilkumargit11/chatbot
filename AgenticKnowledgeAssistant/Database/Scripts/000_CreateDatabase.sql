IF DB_ID(N'Ajay_DB') IS NULL
BEGIN
    CREATE DATABASE Ajay_DB;
END;
GO

DECLARE @SqlServerMajorVersion INT = TRY_CAST(SERVERPROPERTY('ProductMajorVersion') AS INT);
DECLARE @CompatibilityLevel INT =
    CASE
        WHEN @SqlServerMajorVersion >= 16 THEN 160
        WHEN @SqlServerMajorVersion = 15 THEN 150
        WHEN @SqlServerMajorVersion = 14 THEN 140
        WHEN @SqlServerMajorVersion = 13 THEN 130
        ELSE 120
    END;

DECLARE @SetCompatibilitySql NVARCHAR(MAX) =
    N'ALTER DATABASE Ajay_DB SET COMPATIBILITY_LEVEL = ' + CONVERT(NVARCHAR(10), @CompatibilityLevel) + N';';

EXEC sys.sp_executesql @SetCompatibilitySql;
GO

ALTER DATABASE Ajay_DB SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO

ALTER DATABASE Ajay_DB SET QUERY_STORE = ON;
GO

USE Ajay_DB;
GO
