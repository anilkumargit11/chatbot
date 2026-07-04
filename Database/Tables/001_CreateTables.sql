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

IF OBJECT_ID(N'dbo.tblAI_Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_Users
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_Users PRIMARY KEY CLUSTERED,
        UserGuid        UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_tblAI_Users_UserGuid DEFAULT NEWSEQUENTIALID(),
        UserName        NVARCHAR(100) NOT NULL,
        Email           NVARCHAR(256) NOT NULL,
        PasswordHash    NVARCHAR(500) NULL,
        DisplayName     NVARCHAR(200) NULL,
        FullName        NVARCHAR(200) NULL,
        MobileNumber    NVARCHAR(30) NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_tblAI_Users_IsActive DEFAULT (1),
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblAI_Users_IsDeleted DEFAULT (0),
        LastLoginDate   DATETIME2(3) NULL,
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_Users_CreatedDate DEFAULT SYSUTCDATETIME(),
        ModifiedDate    DATETIME2(3) NULL,
        CreatedBy       INT NULL,
        ModifiedBy      INT NULL,
        RowVersion      ROWVERSION NOT NULL,
        CONSTRAINT UQ_tblAI_Users_UserGuid UNIQUE NONCLUSTERED (UserGuid),
        CONSTRAINT UQ_tblAI_Users_UserName UNIQUE NONCLUSTERED (UserName),
        CONSTRAINT UQ_tblAI_Users_Email UNIQUE NONCLUSTERED (Email)
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_Roles
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_Roles PRIMARY KEY CLUSTERED,
        RoleName        NVARCHAR(100) NOT NULL,
        Description     NVARCHAR(500) NULL,
        IsSystemRole    BIT NOT NULL CONSTRAINT DF_tblAI_Roles_IsSystemRole DEFAULT (0),
        IsActive        BIT NOT NULL CONSTRAINT DF_tblAI_Roles_IsActive DEFAULT (1),
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblAI_Roles_IsDeleted DEFAULT (0),
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_Roles_CreatedDate DEFAULT SYSUTCDATETIME(),
        ModifiedDate    DATETIME2(3) NULL,
        CreatedBy       INT NULL,
        ModifiedBy      INT NULL,
        RowVersion      ROWVERSION NOT NULL,
        CONSTRAINT UQ_tblAI_Roles_RoleName UNIQUE NONCLUSTERED (RoleName)
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_Permissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_Permissions
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_Permissions PRIMARY KEY CLUSTERED,
        PermissionName  NVARCHAR(100) NOT NULL,
        Description     NVARCHAR(500) NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_tblAI_Permissions_IsActive DEFAULT (1),
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblAI_Permissions_IsDeleted DEFAULT (0),
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_Permissions_CreatedDate DEFAULT SYSUTCDATETIME(),
        ModifiedDate    DATETIME2(3) NULL,
        CreatedBy       INT NULL,
        ModifiedBy      INT NULL,
        RowVersion      ROWVERSION NOT NULL,
        CONSTRAINT UQ_tblAI_Permissions_PermissionName UNIQUE NONCLUSTERED (PermissionName)
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_UserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_UserRoles
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_UserRoles PRIMARY KEY CLUSTERED,
        UserId          INT NOT NULL,
        RoleId          INT NOT NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_tblAI_UserRoles_IsActive DEFAULT (1),
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblAI_UserRoles_IsDeleted DEFAULT (0),
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_UserRoles_CreatedDate DEFAULT SYSUTCDATETIME(),
        ModifiedDate    DATETIME2(3) NULL,
        CreatedBy       INT NULL,
        ModifiedBy      INT NULL,
        RowVersion      ROWVERSION NOT NULL,
        CONSTRAINT FK_tblAI_UserRoles_tblAI_Users FOREIGN KEY (UserId) REFERENCES dbo.tblAI_Users(Id),
        CONSTRAINT FK_tblAI_UserRoles_tblAI_Roles FOREIGN KEY (RoleId) REFERENCES dbo.tblAI_Roles(Id),
        CONSTRAINT UQ_tblAI_UserRoles_UserId_RoleId UNIQUE NONCLUSTERED (UserId, RoleId)
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_RolePermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_RolePermissions
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_RolePermissions PRIMARY KEY CLUSTERED,
        RoleId          INT NOT NULL,
        PermissionId    INT NOT NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_tblAI_RolePermissions_IsActive DEFAULT (1),
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblAI_RolePermissions_IsDeleted DEFAULT (0),
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_RolePermissions_CreatedDate DEFAULT SYSUTCDATETIME(),
        ModifiedDate    DATETIME2(3) NULL,
        CreatedBy       INT NULL,
        ModifiedBy      INT NULL,
        RowVersion      ROWVERSION NOT NULL,
        CONSTRAINT FK_tblAI_RolePermissions_tblAI_Roles FOREIGN KEY (RoleId) REFERENCES dbo.tblAI_Roles(Id),
        CONSTRAINT FK_tblAI_RolePermissions_tblAI_Permissions FOREIGN KEY (PermissionId) REFERENCES dbo.tblAI_Permissions(Id),
        CONSTRAINT UQ_tblAI_RolePermissions_RoleId_PermissionId UNIQUE NONCLUSTERED (RoleId, PermissionId)
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_KnowledgeBase', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_KnowledgeBase
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_KnowledgeBase PRIMARY KEY CLUSTERED,
        Name            NVARCHAR(200) NOT NULL,
        Description     NVARCHAR(1000) NULL,
        OwnerUserId     INT NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_tblAI_KnowledgeBase_IsActive DEFAULT (1),
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblAI_KnowledgeBase_IsDeleted DEFAULT (0),
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_KnowledgeBase_CreatedDate DEFAULT SYSUTCDATETIME(),
        ModifiedDate    DATETIME2(3) NULL,
        CreatedBy       INT NULL,
        ModifiedBy      INT NULL,
        RowVersion      ROWVERSION NOT NULL,
        CONSTRAINT FK_tblAI_KnowledgeBase_tblAI_Users FOREIGN KEY (OwnerUserId) REFERENCES dbo.tblAI_Users(Id),
        CONSTRAINT UQ_tblAI_KnowledgeBase_Name UNIQUE NONCLUSTERED (Name)
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_AgentConfigurations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_AgentConfigurations
    (
        Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_AgentConfigurations PRIMARY KEY CLUSTERED,
        AgentName           NVARCHAR(150) NOT NULL,
        KnowledgeBaseId     INT NULL,
        ModelName           NVARCHAR(100) NOT NULL CONSTRAINT DF_tblAI_AgentConfigurations_ModelName DEFAULT N'gpt-4o-mini',
        SystemPrompt        NVARCHAR(MAX) NULL,
        Temperature         DECIMAL(4,2) NOT NULL CONSTRAINT DF_tblAI_AgentConfigurations_Temperature DEFAULT (0.20),
        MaxTokens           INT NOT NULL CONSTRAINT DF_tblAI_AgentConfigurations_MaxTokens DEFAULT (1000),
        IsDefault           BIT NOT NULL CONSTRAINT DF_tblAI_AgentConfigurations_IsDefault DEFAULT (0),
        IsActive            BIT NOT NULL CONSTRAINT DF_tblAI_AgentConfigurations_IsActive DEFAULT (1),
        IsDeleted           BIT NOT NULL CONSTRAINT DF_tblAI_AgentConfigurations_IsDeleted DEFAULT (0),
        CreatedDate         DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_AgentConfigurations_CreatedDate DEFAULT SYSUTCDATETIME(),
        ModifiedDate        DATETIME2(3) NULL,
        CreatedBy           INT NULL,
        ModifiedBy          INT NULL,
        RowVersion          ROWVERSION NOT NULL,
        CONSTRAINT FK_tblAI_AgentConfigurations_tblAI_KnowledgeBase FOREIGN KEY (KnowledgeBaseId) REFERENCES dbo.tblAI_KnowledgeBase(Id),
        CONSTRAINT CK_tblAI_AgentConfigurations_Temperature CHECK (Temperature >= 0 AND Temperature <= 2),
        CONSTRAINT CK_tblAI_AgentConfigurations_MaxTokens CHECK (MaxTokens BETWEEN 1 AND 128000)
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_ChatSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_ChatSessions
    (
        Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_ChatSessions PRIMARY KEY CLUSTERED,
        SessionGuid         UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_tblAI_ChatSessions_SessionGuid DEFAULT NEWSEQUENTIALID(),
        UserId              INT NULL,
        AgentConfigurationId INT NULL,
        Title               NVARCHAR(250) NULL,
        IsArchived          BIT NOT NULL CONSTRAINT DF_tblAI_ChatSessions_IsArchived DEFAULT (0),
        IsActive            BIT NOT NULL CONSTRAINT DF_tblAI_ChatSessions_IsActive DEFAULT (1),
        IsDeleted           BIT NOT NULL CONSTRAINT DF_tblAI_ChatSessions_IsDeleted DEFAULT (0),
        CreatedDate         DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_ChatSessions_CreatedDate DEFAULT SYSUTCDATETIME(),
        ModifiedDate        DATETIME2(3) NULL,
        CreatedBy           INT NULL,
        ModifiedBy          INT NULL,
        RowVersion          ROWVERSION NOT NULL,
        CONSTRAINT UQ_tblAI_ChatSessions_SessionGuid UNIQUE NONCLUSTERED (SessionGuid),
        CONSTRAINT FK_tblAI_ChatSessions_tblAI_Users FOREIGN KEY (UserId) REFERENCES dbo.tblAI_Users(Id),
        CONSTRAINT FK_tblAI_ChatSessions_tblAI_AgentConfigurations FOREIGN KEY (AgentConfigurationId) REFERENCES dbo.tblAI_AgentConfigurations(Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_ChatMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_ChatMessages
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_ChatMessages PRIMARY KEY CLUSTERED,
        SessionId       INT NOT NULL,
        UserId          INT NULL,
        Question        NVARCHAR(MAX) NULL,
        Response        NVARCHAR(MAX) NULL,
        PromptTokens    INT NULL,
        CompletionTokens INT NULL,
        TotalTokens     AS (ISNULL(PromptTokens, 0) + ISNULL(CompletionTokens, 0)) PERSISTED,
        ModelName       NVARCHAR(100) NULL,
        LatencyMs       INT NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_tblAI_ChatMessages_IsActive DEFAULT (1),
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblAI_ChatMessages_IsDeleted DEFAULT (0),
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_ChatMessages_CreatedDate DEFAULT SYSUTCDATETIME(),
        ModifiedDate    DATETIME2(3) NULL,
        CreatedBy       INT NULL,
        ModifiedBy      INT NULL,
        RowVersion      ROWVERSION NOT NULL,
        CONSTRAINT FK_tblAI_ChatMessages_tblAI_ChatSessions FOREIGN KEY (SessionId) REFERENCES dbo.tblAI_ChatSessions(Id),
        CONSTRAINT FK_tblAI_ChatMessages_tblAI_Users FOREIGN KEY (UserId) REFERENCES dbo.tblAI_Users(Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_Documents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_Documents
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_Documents PRIMARY KEY CLUSTERED,
        KnowledgeBaseId INT NULL,
        Title           NVARCHAR(500) NOT NULL,
        FileName        NVARCHAR(260) NULL,
        FileExtension   NVARCHAR(20) NULL,
        ContentType     NVARCHAR(150) NULL,
        FileSizeBytes   BIGINT NULL,
        Content         NVARCHAR(MAX) NOT NULL,
        ContentHash     VARBINARY(32) NULL,
        ProcessingStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_tblAI_Documents_ProcessingStatus DEFAULT N'Completed',
        IsActive        BIT NOT NULL CONSTRAINT DF_tblAI_Documents_IsActive DEFAULT (1),
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblAI_Documents_IsDeleted DEFAULT (0),
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_Documents_CreatedDate DEFAULT SYSUTCDATETIME(),
        ModifiedDate    DATETIME2(3) NULL,
        CreatedBy       INT NULL,
        ModifiedBy      INT NULL,
        RowVersion      ROWVERSION NOT NULL,
        CONSTRAINT FK_tblAI_Documents_tblAI_KnowledgeBase FOREIGN KEY (KnowledgeBaseId) REFERENCES dbo.tblAI_KnowledgeBase(Id),
        CONSTRAINT CK_tblAI_Documents_ProcessingStatus CHECK (ProcessingStatus IN (N'Pending', N'Processing', N'Completed', N'Failed'))
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_DocumentChunks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_DocumentChunks
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_DocumentChunks PRIMARY KEY CLUSTERED,
        DocumentId      INT NOT NULL,
        ChunkIndex      INT NOT NULL,
        Content         NVARCHAR(MAX) NOT NULL,
        TokenCount      INT NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_tblAI_DocumentChunks_IsActive DEFAULT (1),
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblAI_DocumentChunks_IsDeleted DEFAULT (0),
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_DocumentChunks_CreatedDate DEFAULT SYSUTCDATETIME(),
        ModifiedDate    DATETIME2(3) NULL,
        CreatedBy       INT NULL,
        ModifiedBy      INT NULL,
        RowVersion      ROWVERSION NOT NULL,
        CONSTRAINT FK_tblAI_DocumentChunks_tblAI_Documents FOREIGN KEY (DocumentId) REFERENCES dbo.tblAI_Documents(Id),
        CONSTRAINT UQ_tblAI_DocumentChunks_DocumentId_ChunkIndex UNIQUE NONCLUSTERED (DocumentId, ChunkIndex)
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_Embeddings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_Embeddings
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_Embeddings PRIMARY KEY CLUSTERED,
        DocumentId      INT NOT NULL,
        DocumentChunkId INT NULL,
        ProviderName    NVARCHAR(100) NOT NULL CONSTRAINT DF_tblAI_Embeddings_ProviderName DEFAULT N'OpenAI',
        ModelName       NVARCHAR(100) NULL,
        VectorData      NVARCHAR(MAX) NOT NULL,
        VectorDimension INT NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_tblAI_Embeddings_IsActive DEFAULT (1),
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblAI_Embeddings_IsDeleted DEFAULT (0),
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_Embeddings_CreatedDate DEFAULT SYSUTCDATETIME(),
        ModifiedDate    DATETIME2(3) NULL,
        CreatedBy       INT NULL,
        ModifiedBy      INT NULL,
        RowVersion      ROWVERSION NOT NULL,
        CONSTRAINT FK_tblAI_Embeddings_tblAI_Documents FOREIGN KEY (DocumentId) REFERENCES dbo.tblAI_Documents(Id),
        CONSTRAINT FK_tblAI_Embeddings_tblAI_DocumentChunks FOREIGN KEY (DocumentChunkId) REFERENCES dbo.tblAI_DocumentChunks(Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_ApplicationLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_ApplicationLogs
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_ApplicationLogs PRIMARY KEY CLUSTERED,
        LogLevel        NVARCHAR(30) NOT NULL,
        Source          NVARCHAR(200) NULL,
        Message         NVARCHAR(MAX) NOT NULL,
        PropertiesJson  NVARCHAR(MAX) NULL,
        TraceId         NVARCHAR(100) NULL,
        UserId          INT NULL,
        CreatedBy       INT NULL,
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_ApplicationLogs_CreatedDate DEFAULT SYSUTCDATETIME(),
        ModifiedBy      INT NULL,
        ModifiedDate    DATETIME2(3) NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_tblAI_ApplicationLogs_IsActive DEFAULT (1),
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblAI_ApplicationLogs_IsDeleted DEFAULT (0),
        CONSTRAINT FK_tblAI_ApplicationLogs_tblAI_Users FOREIGN KEY (UserId) REFERENCES dbo.tblAI_Users(Id),
        CONSTRAINT CK_tblAI_ApplicationLogs_LogLevel CHECK (LogLevel IN (N'Trace', N'Debug', N'Information', N'Warning', N'Error', N'Critical'))
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_ErrorLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_ErrorLogs
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_ErrorLogs PRIMARY KEY CLUSTERED,
        ErrorGuid       UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_tblAI_ErrorLogs_ErrorGuid DEFAULT NEWSEQUENTIALID(),
        Source          NVARCHAR(200) NULL,
        ErrorMessage    NVARCHAR(MAX) NOT NULL,
        StackTrace      NVARCHAR(MAX) NULL,
        RequestPath     NVARCHAR(500) NULL,
        HttpMethod      NVARCHAR(20) NULL,
        StatusCode      INT NULL,
        TraceId         NVARCHAR(100) NULL,
        UserId          INT NULL,
        IsResolved      BIT NOT NULL CONSTRAINT DF_tblAI_ErrorLogs_IsResolved DEFAULT (0),
        CreatedBy       INT NULL,
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_ErrorLogs_CreatedDate DEFAULT SYSUTCDATETIME(),
        ModifiedBy      INT NULL,
        ModifiedDate    DATETIME2(3) NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_tblAI_ErrorLogs_IsActive DEFAULT (1),
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblAI_ErrorLogs_IsDeleted DEFAULT (0),
        CONSTRAINT UQ_tblAI_ErrorLogs_ErrorGuid UNIQUE NONCLUSTERED (ErrorGuid),
        CONSTRAINT FK_tblAI_ErrorLogs_tblAI_Users FOREIGN KEY (UserId) REFERENCES dbo.tblAI_Users(Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_AuditLogs
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_AuditLogs PRIMARY KEY CLUSTERED,
        TableName       SYSNAME NOT NULL,
        RecordId        NVARCHAR(100) NOT NULL,
        ActionType      NVARCHAR(20) NOT NULL,
        OldValuesJson   NVARCHAR(MAX) NULL,
        NewValuesJson   NVARCHAR(MAX) NULL,
        UserId          INT NULL,
        CreatedBy       INT NULL,
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_AuditLogs_CreatedDate DEFAULT SYSUTCDATETIME(),
        ModifiedBy      INT NULL,
        ModifiedDate    DATETIME2(3) NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_tblAI_AuditLogs_IsActive DEFAULT (1),
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblAI_AuditLogs_IsDeleted DEFAULT (0),
        CONSTRAINT FK_tblAI_AuditLogs_tblAI_Users FOREIGN KEY (UserId) REFERENCES dbo.tblAI_Users(Id),
        CONSTRAINT CK_tblAI_AuditLogs_ActionType CHECK (ActionType IN (N'INSERT', N'UPDATE', N'DELETE', N'SOFT_DELETE'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tblAI_Roles WHERE RoleName = N'Administrator')
    INSERT INTO dbo.tblAI_Roles (RoleName, Description, IsSystemRole) VALUES (N'Administrator', N'Full system access.', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tblAI_Roles WHERE RoleName = N'User')
    INSERT INTO dbo.tblAI_Roles (RoleName, Description, IsSystemRole) VALUES (N'User', N'Standard application access.', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.tblAI_KnowledgeBase WHERE Name = N'Default')
    INSERT INTO dbo.tblAI_KnowledgeBase (Name, Description) VALUES (N'Default', N'Default knowledge base for uploaded documents.');

IF NOT EXISTS (SELECT 1 FROM dbo.tblAI_AgentConfigurations WHERE IsDefault = 1)
    INSERT INTO dbo.tblAI_AgentConfigurations (AgentName, KnowledgeBaseId, ModelName, SystemPrompt, IsDefault)
    SELECT N'Default Assistant', Id, N'gpt-4o-mini', N'Answer using the available knowledge base context. Be concise and cite relevant document titles when possible.', 1
    FROM dbo.tblAI_KnowledgeBase
    WHERE Name = N'Default';
GO
