SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.tblAI_RagSearchHistory', N'U') IS NOT NULL DROP TABLE dbo.tblAI_RagSearchHistory;
GO
IF OBJECT_ID(N'dbo.tblAI_RagChunkMetadata', N'U') IS NOT NULL DROP TABLE dbo.tblAI_RagChunkMetadata;
GO
IF OBJECT_ID(N'dbo.tblAI_RagVectorIndex', N'U') IS NOT NULL DROP TABLE dbo.tblAI_RagVectorIndex;
GO
IF OBJECT_ID(N'dbo.tblAI_RagEmbeddings', N'U') IS NOT NULL DROP TABLE dbo.tblAI_RagEmbeddings;
GO
IF OBJECT_ID(N'dbo.tblAI_RagChunks', N'U') IS NOT NULL DROP TABLE dbo.tblAI_RagChunks;
GO
IF OBJECT_ID(N'dbo.tblAI_RagDocuments', N'U') IS NOT NULL DROP TABLE dbo.tblAI_RagDocuments;
GO
