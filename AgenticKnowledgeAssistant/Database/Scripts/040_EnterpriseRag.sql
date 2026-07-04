SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.tblAI_RagDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_RagDocuments
    (
        DocumentId       BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_RagDocuments PRIMARY KEY CLUSTERED,
        UserId           INT NOT NULL,
        FileName         NVARCHAR(260) NOT NULL,
        ContentType      NVARCHAR(150) NOT NULL,
        FileSizeBytes    BIGINT NOT NULL,
        Title            NVARCHAR(500) NOT NULL,
        ProcessingStatus NVARCHAR(50) NOT NULL CONSTRAINT DF_tblAI_RagDocuments_Status DEFAULT N'Uploaded',
        ChunkCount       INT NOT NULL CONSTRAINT DF_tblAI_RagDocuments_ChunkCount DEFAULT (0),
        EmbeddingCount   INT NOT NULL CONSTRAINT DF_tblAI_RagDocuments_EmbeddingCount DEFAULT (0),
        Summary          NVARCHAR(MAX) NULL,
        Metadata         NVARCHAR(MAX) NULL,
        IsDeleted        BIT NOT NULL CONSTRAINT DF_tblAI_RagDocuments_IsDeleted DEFAULT (0),
        CreatedDate      DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_RagDocuments_CreatedDate DEFAULT SYSUTCDATETIME(),
        UpdatedDate      DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_RagDocuments_UpdatedDate DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_tblAI_RagDocuments_tblAI_Users FOREIGN KEY (UserId) REFERENCES dbo.tblAI_Users(Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_RagChunks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_RagChunks
    (
        ChunkId     BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_RagChunks PRIMARY KEY CLUSTERED,
        DocumentId  BIGINT NOT NULL,
        ChunkIndex  INT NOT NULL,
        PageNumber  INT NULL,
        Section     NVARCHAR(300) NULL,
        Heading     NVARCHAR(300) NULL,
        Content     NVARCHAR(MAX) NOT NULL,
        TokenCount  INT NOT NULL,
        Metadata    NVARCHAR(MAX) NULL,
        CreatedDate DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_RagChunks_CreatedDate DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_tblAI_RagChunks_tblAI_RagDocuments FOREIGN KEY (DocumentId) REFERENCES dbo.tblAI_RagDocuments(DocumentId),
        CONSTRAINT UQ_tblAI_RagChunks_Document_Chunk UNIQUE NONCLUSTERED (DocumentId, ChunkIndex)
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_RagEmbeddings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_RagEmbeddings
    (
        EmbeddingId     BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_RagEmbeddings PRIMARY KEY CLUSTERED,
        ChunkId         BIGINT NOT NULL,
        ProviderName    NVARCHAR(100) NOT NULL,
        ModelName       NVARCHAR(150) NOT NULL,
        VectorData      NVARCHAR(MAX) NOT NULL,
        VectorDimension INT NOT NULL,
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_RagEmbeddings_CreatedDate DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_tblAI_RagEmbeddings_tblAI_RagChunks FOREIGN KEY (ChunkId) REFERENCES dbo.tblAI_RagChunks(ChunkId)
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_RagVectorIndex', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_RagVectorIndex
    (
        VectorIndexId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_RagVectorIndex PRIMARY KEY CLUSTERED,
        ProviderName  NVARCHAR(100) NOT NULL,
        IndexName     NVARCHAR(200) NOT NULL,
        Endpoint      NVARCHAR(500) NULL,
        IsActive      BIT NOT NULL CONSTRAINT DF_tblAI_RagVectorIndex_IsActive DEFAULT (1),
        CreatedDate   DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_RagVectorIndex_CreatedDate DEFAULT SYSUTCDATETIME()
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_RagChunkMetadata', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_RagChunkMetadata
    (
        MetadataId  BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_RagChunkMetadata PRIMARY KEY CLUSTERED,
        ChunkId     BIGINT NOT NULL,
        [Key]       NVARCHAR(150) NOT NULL,
        [Value]     NVARCHAR(1000) NOT NULL,
        CreatedDate DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_RagChunkMetadata_CreatedDate DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_tblAI_RagChunkMetadata_tblAI_RagChunks FOREIGN KEY (ChunkId) REFERENCES dbo.tblAI_RagChunks(ChunkId)
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_RagSearchHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_RagSearchHistory
    (
        SearchHistoryId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_RagSearchHistory PRIMARY KEY CLUSTERED,
        UserId          INT NOT NULL,
        Query           NVARCHAR(2000) NOT NULL,
        ResultCount     INT NOT NULL,
        SearchType      NVARCHAR(50) NOT NULL,
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_RagSearchHistory_CreatedDate DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_tblAI_RagSearchHistory_tblAI_Users FOREIGN KEY (UserId) REFERENCES dbo.tblAI_Users(Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tblAI_RagDocuments_User_Status' AND object_id = OBJECT_ID(N'dbo.tblAI_RagDocuments'))
    CREATE INDEX IX_tblAI_RagDocuments_User_Status ON dbo.tblAI_RagDocuments (UserId, IsDeleted, ProcessingStatus, UpdatedDate DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tblAI_RagChunks_Document' AND object_id = OBJECT_ID(N'dbo.tblAI_RagChunks'))
    CREATE INDEX IX_tblAI_RagChunks_Document ON dbo.tblAI_RagChunks (DocumentId, ChunkIndex) INCLUDE (PageNumber, TokenCount);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tblAI_RagEmbeddings_Chunk' AND object_id = OBJECT_ID(N'dbo.tblAI_RagEmbeddings'))
    CREATE INDEX IX_tblAI_RagEmbeddings_Chunk ON dbo.tblAI_RagEmbeddings (ChunkId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tblAI_RagSearchHistory_User_Date' AND object_id = OBJECT_ID(N'dbo.tblAI_RagSearchHistory'))
    CREATE INDEX IX_tblAI_RagSearchHistory_User_Date ON dbo.tblAI_RagSearchHistory (UserId, CreatedDate DESC);
GO
