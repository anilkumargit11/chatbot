SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.tblAI_ConversationSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_ConversationSessions
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_ConversationSessions PRIMARY KEY CLUSTERED,
        SessionGuid     UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_tblAI_ConversationSessions_SessionGuid DEFAULT NEWID(),
        UserId          INT NOT NULL,
        Title           NVARCHAR(250) NOT NULL,
        Status          NVARCHAR(30) NOT NULL CONSTRAINT DF_tblAI_ConversationSessions_Status DEFAULT N'Active',
        IsPinned        BIT NOT NULL CONSTRAINT DF_tblAI_ConversationSessions_IsPinned DEFAULT (0),
        IsFavorite      BIT NOT NULL CONSTRAINT DF_tblAI_ConversationSessions_IsFavorite DEFAULT (0),
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_ConversationSessions_CreatedDate DEFAULT SYSUTCDATETIME(),
        UpdatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_ConversationSessions_UpdatedDate DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_tblAI_ConversationSessions_SessionGuid UNIQUE NONCLUSTERED (SessionGuid),
        CONSTRAINT FK_tblAI_ConversationSessions_tblAI_Users FOREIGN KEY (UserId) REFERENCES dbo.tblAI_Users(Id),
        CONSTRAINT CK_tblAI_ConversationSessions_Status CHECK (Status IN (N'Active', N'Archived', N'Deleted'))
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_ConversationMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_ConversationMessages
    (
        MessageId       BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_ConversationMessages PRIMARY KEY CLUSTERED,
        SessionId       INT NOT NULL,
        Role            NVARCHAR(20) NOT NULL,
        Message         NVARCHAR(MAX) NOT NULL,
        Tokens          INT NULL,
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_ConversationMessages_CreatedDate DEFAULT SYSUTCDATETIME(),
        Metadata        NVARCHAR(MAX) NULL,
        CONSTRAINT FK_tblAI_ConversationMessages_tblAI_ConversationSessions FOREIGN KEY (SessionId) REFERENCES dbo.tblAI_ConversationSessions(Id),
        CONSTRAINT CK_tblAI_ConversationMessages_Role CHECK (Role IN (N'User', N'Assistant', N'System')),
        CONSTRAINT CK_tblAI_ConversationMessages_Tokens CHECK (Tokens IS NULL OR Tokens >= 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tblAI_ConversationSessions_User_Updated' AND object_id = OBJECT_ID(N'dbo.tblAI_ConversationSessions'))
    CREATE INDEX IX_tblAI_ConversationSessions_User_Updated ON dbo.tblAI_ConversationSessions (UserId, Status, IsPinned DESC, UpdatedDate DESC) INCLUDE (SessionGuid, Title, IsFavorite);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tblAI_ConversationSessions_User_Favorite' AND object_id = OBJECT_ID(N'dbo.tblAI_ConversationSessions'))
    CREATE INDEX IX_tblAI_ConversationSessions_User_Favorite ON dbo.tblAI_ConversationSessions (UserId, IsFavorite, UpdatedDate DESC) WHERE Status <> N'Deleted';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tblAI_ConversationMessages_Session_Created' AND object_id = OBJECT_ID(N'dbo.tblAI_ConversationMessages'))
    CREATE INDEX IX_tblAI_ConversationMessages_Session_Created ON dbo.tblAI_ConversationMessages (SessionId, CreatedDate DESC, MessageId DESC) INCLUDE (Role, Tokens);
GO
