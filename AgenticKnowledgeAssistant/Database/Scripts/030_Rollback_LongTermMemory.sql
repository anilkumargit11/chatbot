SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.tblAI_UserMemory', N'U') IS NOT NULL
    DROP TABLE dbo.tblAI_UserMemory;
GO

IF OBJECT_ID(N'dbo.tblAI_MemoryCategory', N'U') IS NOT NULL
    DROP TABLE dbo.tblAI_MemoryCategory;
GO
