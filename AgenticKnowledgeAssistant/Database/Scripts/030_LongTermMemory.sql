SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.tblAI_MemoryCategory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_MemoryCategory
    (
        CategoryId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_MemoryCategory PRIMARY KEY CLUSTERED,
        CategoryName    NVARCHAR(100) NOT NULL,
        Description     NVARCHAR(500) NULL,
        SortOrder       INT NOT NULL CONSTRAINT DF_tblAI_MemoryCategory_SortOrder DEFAULT (100),
        IsActive        BIT NOT NULL CONSTRAINT DF_tblAI_MemoryCategory_IsActive DEFAULT (1),
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_MemoryCategory_CreatedDate DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_tblAI_MemoryCategory_CategoryName UNIQUE NONCLUSTERED (CategoryName)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tblAI_MemoryCategory WHERE CategoryName = N'User Preferences')
    INSERT INTO dbo.tblAI_MemoryCategory (CategoryName, Description, SortOrder) VALUES (N'User Preferences', N'User-approved response, language, theme, provider, and coding preferences.', 10);
IF NOT EXISTS (SELECT 1 FROM dbo.tblAI_MemoryCategory WHERE CategoryName = N'Project Memory')
    INSERT INTO dbo.tblAI_MemoryCategory (CategoryName, Description, SortOrder) VALUES (N'Project Memory', N'Current projects, frameworks, standards, and reusable project context.', 20);
IF NOT EXISTS (SELECT 1 FROM dbo.tblAI_MemoryCategory WHERE CategoryName = N'Workspace Memory')
    INSERT INTO dbo.tblAI_MemoryCategory (CategoryName, Description, SortOrder) VALUES (N'Workspace Memory', N'Organization, department, workspace, and environment preferences.', 30);
IF NOT EXISTS (SELECT 1 FROM dbo.tblAI_MemoryCategory WHERE CategoryName = N'Application Settings')
    INSERT INTO dbo.tblAI_MemoryCategory (CategoryName, Description, SortOrder) VALUES (N'Application Settings', N'Approved application-level settings that personalize AI behavior.', 40);
IF NOT EXISTS (SELECT 1 FROM dbo.tblAI_MemoryCategory WHERE CategoryName = N'Favorite Items')
    INSERT INTO dbo.tblAI_MemoryCategory (CategoryName, Description, SortOrder) VALUES (N'Favorite Items', N'Favorite APIs, databases, prompts, commands, providers, and technologies.', 50);
IF NOT EXISTS (SELECT 1 FROM dbo.tblAI_MemoryCategory WHERE CategoryName = N'Pinned Knowledge')
    INSERT INTO dbo.tblAI_MemoryCategory (CategoryName, Description, SortOrder) VALUES (N'Pinned Knowledge', N'Pinned documents, prompts, and knowledge snippets.', 60);
IF NOT EXISTS (SELECT 1 FROM dbo.tblAI_MemoryCategory WHERE CategoryName = N'Reusable Context')
    INSERT INTO dbo.tblAI_MemoryCategory (CategoryName, Description, SortOrder) VALUES (N'Reusable Context', N'Reusable facts that the user explicitly asked the AI to remember.', 70);
GO

IF OBJECT_ID(N'dbo.tblAI_UserMemory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_UserMemory
    (
        MemoryId        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_UserMemory PRIMARY KEY CLUSTERED,
        UserId          INT NOT NULL,
        CategoryId      INT NOT NULL,
        [Key]           NVARCHAR(150) NOT NULL,
        [Value]         NVARCHAR(MAX) NOT NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_tblAI_UserMemory_IsActive DEFAULT (1),
        IsPinned        BIT NOT NULL CONSTRAINT DF_tblAI_UserMemory_IsPinned DEFAULT (0),
        IsFavorite      BIT NOT NULL CONSTRAINT DF_tblAI_UserMemory_IsFavorite DEFAULT (0),
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_UserMemory_CreatedDate DEFAULT SYSUTCDATETIME(),
        UpdatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_UserMemory_UpdatedDate DEFAULT SYSUTCDATETIME(),
        Metadata        NVARCHAR(MAX) NULL,
        CONSTRAINT FK_tblAI_UserMemory_tblAI_Users FOREIGN KEY (UserId) REFERENCES dbo.tblAI_Users(Id),
        CONSTRAINT FK_tblAI_UserMemory_tblAI_MemoryCategory FOREIGN KEY (CategoryId) REFERENCES dbo.tblAI_MemoryCategory(CategoryId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tblAI_UserMemory_User_Category_Updated' AND object_id = OBJECT_ID(N'dbo.tblAI_UserMemory'))
    CREATE INDEX IX_tblAI_UserMemory_User_Category_Updated ON dbo.tblAI_UserMemory (UserId, CategoryId, IsActive, UpdatedDate DESC) INCLUDE ([Key], IsPinned, IsFavorite);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tblAI_UserMemory_User_Pinned' AND object_id = OBJECT_ID(N'dbo.tblAI_UserMemory'))
    CREATE INDEX IX_tblAI_UserMemory_User_Pinned ON dbo.tblAI_UserMemory (UserId, IsPinned DESC, IsFavorite DESC, UpdatedDate DESC) WHERE IsActive = 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tblAI_UserMemory_User_Key' AND object_id = OBJECT_ID(N'dbo.tblAI_UserMemory'))
    CREATE INDEX IX_tblAI_UserMemory_User_Key ON dbo.tblAI_UserMemory (UserId, [Key]);
GO
