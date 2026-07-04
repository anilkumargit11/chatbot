USE Ajay_DB;
GO

-- 1. MFA Settings Table
IF OBJECT_ID(N'dbo.tblAI_UserMfaSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_UserMfaSettings
    (
        Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_UserMfaSettings PRIMARY KEY CLUSTERED,
        UserId              INT NOT NULL,
        EmailOtpEnabled     BIT NOT NULL CONSTRAINT DF_tblAI_UserMfaSettings_EmailOtpEnabled DEFAULT (0),
        SmsOtpEnabled       BIT NOT NULL CONSTRAINT DF_tblAI_UserMfaSettings_SmsOtpEnabled DEFAULT (0),
        AuthenticatorSecret NVARCHAR(128) NULL,
        IsMfaConfigured     BIT NOT NULL CONSTRAINT DF_tblAI_UserMfaSettings_IsMfaConfigured DEFAULT (0),
        BackupCodes         NVARCHAR(500) NULL, -- comma separated encrypted/hashed backup codes
        CreatedDate         DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_UserMfaSettings_CreatedDate DEFAULT SYSUTCDATETIME(),
        ModifiedDate        DATETIME2(3) NULL,
        CONSTRAINT FK_tblAI_UserMfaSettings_tblAI_Users FOREIGN KEY (UserId) REFERENCES dbo.tblAI_Users(Id),
        CONSTRAINT UQ_tblAI_UserMfaSettings_UserId UNIQUE NONCLUSTERED (UserId)
    );
END;
GO

-- 2. Folders Table
IF OBJECT_ID(N'dbo.tblAI_Folders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_Folders
    (
        Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_Folders PRIMARY KEY CLUSTERED,
        UserId              INT NOT NULL,
        FolderName          NVARCHAR(150) NOT NULL,
        CreatedDate         DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_Folders_CreatedDate DEFAULT SYSUTCDATETIME(),
        IsActive            BIT NOT NULL CONSTRAINT DF_tblAI_Folders_IsActive DEFAULT (1),
        IsDeleted           BIT NOT NULL CONSTRAINT DF_tblAI_Folders_IsDeleted DEFAULT (0),
        CONSTRAINT FK_tblAI_Folders_tblAI_Users FOREIGN KEY (UserId) REFERENCES dbo.tblAI_Users(Id)
    );
END;
GO

-- 3. Alter tblAI_ChatSessions for Folders, Pins, ShareGuid, and Language
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblAI_ChatSessions') AND name = N'FolderId')
BEGIN
    ALTER TABLE dbo.tblAI_ChatSessions ADD FolderId INT NULL CONSTRAINT FK_tblAI_ChatSessions_tblAI_Folders FOREIGN KEY REFERENCES dbo.tblAI_Folders(Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblAI_ChatSessions') AND name = N'IsPinned')
BEGIN
    ALTER TABLE dbo.tblAI_ChatSessions ADD IsPinned BIT NOT NULL CONSTRAINT DF_tblAI_ChatSessions_IsPinned DEFAULT (0);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblAI_ChatSessions') AND name = N'ShareGuid')
BEGIN
    ALTER TABLE dbo.tblAI_ChatSessions ADD ShareGuid UNIQUEIDENTIFIER NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.tblAI_ChatSessions') AND name = N'LanguageCode')
BEGIN
    ALTER TABLE dbo.tblAI_ChatSessions ADD LanguageCode NVARCHAR(10) NOT NULL CONSTRAINT DF_tblAI_ChatSessions_LanguageCode DEFAULT N'en';
END;
GO

-- 4. Tags Tables
IF OBJECT_ID(N'dbo.tblAI_Tags', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_Tags
    (
        Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_Tags PRIMARY KEY CLUSTERED,
        TagName             NVARCHAR(50) NOT NULL,
        CreatedDate         DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_Tags_CreatedDate DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_tblAI_Tags_TagName UNIQUE NONCLUSTERED (TagName)
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_SessionTags', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_SessionTags
    (
        Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_SessionTags PRIMARY KEY CLUSTERED,
        SessionId           INT NOT NULL,
        TagId               INT NOT NULL,
        CreatedDate         DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_SessionTags_CreatedDate DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_tblAI_SessionTags_tblAI_ChatSessions FOREIGN KEY (SessionId) REFERENCES dbo.tblAI_ChatSessions(Id),
        CONSTRAINT FK_tblAI_SessionTags_tblAI_Tags FOREIGN KEY (TagId) REFERENCES dbo.tblAI_Tags(Id),
        CONSTRAINT UQ_tblAI_SessionTags_SessionId_TagId UNIQUE NONCLUSTERED (SessionId, TagId)
    );
END;
GO
