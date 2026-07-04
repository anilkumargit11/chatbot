USE Ajay_DB;
GO

-- 1. Get User MFA Settings
CREATE OR ALTER PROCEDURE dbo.usp_AI_GetMfaSettings
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT UserId, EmailOtpEnabled, SmsOtpEnabled, AuthenticatorSecret, IsMfaConfigured, BackupCodes
    FROM dbo.tblAI_UserMfaSettings
    WHERE UserId = @UserId;
END;
GO

-- 2. Save/Update User MFA Settings
CREATE OR ALTER PROCEDURE dbo.usp_AI_SaveMfaSettings
    @UserId INT,
    @EmailOtpEnabled BIT,
    @SmsOtpEnabled BIT,
    @AuthenticatorSecret NVARCHAR(128) = NULL,
    @IsMfaConfigured BIT,
    @BackupCodes NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tblAI_UserMfaSettings WHERE UserId = @UserId)
    BEGIN
        UPDATE dbo.tblAI_UserMfaSettings
        SET EmailOtpEnabled = @EmailOtpEnabled,
            SmsOtpEnabled = @SmsOtpEnabled,
            AuthenticatorSecret = COALESCE(@AuthenticatorSecret, AuthenticatorSecret),
            IsMfaConfigured = @IsMfaConfigured,
            BackupCodes = COALESCE(@BackupCodes, BackupCodes),
            ModifiedDate = SYSUTCDATETIME()
        WHERE UserId = @UserId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.tblAI_UserMfaSettings (UserId, EmailOtpEnabled, SmsOtpEnabled, AuthenticatorSecret, IsMfaConfigured, BackupCodes)
        VALUES (@UserId, @EmailOtpEnabled, @SmsOtpEnabled, @AuthenticatorSecret, @IsMfaConfigured, @BackupCodes);
    END;
END;
GO

-- 3. Create Folder
CREATE OR ALTER PROCEDURE dbo.usp_AI_CreateFolder
    @UserId INT,
    @FolderName NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tblAI_Folders (UserId, FolderName)
    VALUES (@UserId, @FolderName);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END;
GO

-- 4. Get Folders
CREATE OR ALTER PROCEDURE dbo.usp_AI_GetFolders
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, FolderName, CreatedDate
    FROM dbo.tblAI_Folders
    WHERE UserId = @UserId AND IsDeleted = 0 AND IsActive = 1
    ORDER BY FolderName;
END;
GO

-- 5. Delete Folder
CREATE OR ALTER PROCEDURE dbo.usp_AI_DeleteFolder
    @FolderId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblAI_Folders
    SET IsDeleted = 1
    WHERE Id = @FolderId AND UserId = @UserId;

    -- Detach sessions from deleted folder
    UPDATE dbo.tblAI_ChatSessions
    SET FolderId = NULL
    WHERE FolderId = @FolderId;
END;
GO

-- 6. Pin Chat Session
CREATE OR ALTER PROCEDURE dbo.usp_AI_PinChatSession
    @SessionId INT,
    @IsPinned BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblAI_ChatSessions
    SET IsPinned = @IsPinned,
        ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @SessionId AND IsDeleted = 0;
END;
GO

-- 7. Share Chat Session
CREATE OR ALTER PROCEDURE dbo.usp_AI_ShareChatSession
    @SessionId INT,
    @ShareGuid UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Guid UNIQUEIDENTIFIER = COALESCE(@ShareGuid, NEWID());

    UPDATE dbo.tblAI_ChatSessions
    SET ShareGuid = @Guid,
        ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @SessionId AND IsDeleted = 0;

    SELECT @Guid AS ShareGuid;
END;
GO

-- 8. Tag Chat Session
CREATE OR ALTER PROCEDURE dbo.usp_AI_TagSession
    @SessionId INT,
    @TagName NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TagId INT;

    -- Ensure tag exists
    SELECT @TagId = Id FROM dbo.tblAI_Tags WHERE TagName = @TagName;
    
    IF @TagId IS NULL
    BEGIN
        INSERT INTO dbo.tblAI_Tags (TagName) VALUES (@TagName);
        SET @TagId = CAST(SCOPE_IDENTITY() AS INT);
    END;

    -- Associate tag
    IF NOT EXISTS (SELECT 1 FROM dbo.tblAI_SessionTags WHERE SessionId = @SessionId AND TagId = @TagId)
    BEGIN
        INSERT INTO dbo.tblAI_SessionTags (SessionId, TagId)
        VALUES (@SessionId, @TagId);
    END;
END;
GO

-- 9. Get Session Tags
CREATE OR ALTER PROCEDURE dbo.usp_AI_GetSessionTags
    @SessionId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT t.Id, t.TagName
    FROM dbo.tblAI_Tags t
    INNER JOIN dbo.tblAI_SessionTags st ON t.Id = st.TagId
    WHERE st.SessionId = @SessionId
    ORDER BY t.TagName;
END;
GO

-- 10. Update Insert Chat Session (Overrides existing to include optional FolderId and LanguageCode)
CREATE OR ALTER PROCEDURE dbo.usp_AI_InsertChatSession
    @UserId INT = NULL,
    @AgentConfigurationId INT = NULL,
    @Title NVARCHAR(250) = NULL,
    @CreatedBy INT = NULL,
    @FolderId INT = NULL,
    @LanguageCode NVARCHAR(10) = N'en'
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tblAI_ChatSessions (UserId, AgentConfigurationId, Title, CreatedBy, FolderId, LanguageCode, IsPinned)
    VALUES (@UserId, @AgentConfigurationId, @Title, @CreatedBy, @FolderId, @LanguageCode, 0);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END;
GO

-- 11. Update Get Chat Sessions (Overrides existing to support pinning, folders, and shared filters)
CREATE OR ALTER PROCEDURE dbo.usp_AI_GetChatSessions
    @UserId INT = NULL,
    @FolderId INT = NULL,
    @IsPinned BIT = NULL,
    @Limit INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Limit)
        s.Id, s.SessionGuid, s.UserId, s.AgentConfigurationId, s.Title, s.IsArchived, 
        s.CreatedDate, s.ModifiedDate, s.FolderId, s.IsPinned, s.ShareGuid, s.LanguageCode,
        f.FolderName
    FROM dbo.tblAI_ChatSessions s
    LEFT JOIN dbo.tblAI_Folders f ON s.FolderId = f.Id
    WHERE s.IsDeleted = 0
      AND (@UserId IS NULL OR s.UserId = @UserId)
      AND (@FolderId IS NULL OR s.FolderId = @FolderId)
      AND (@IsPinned IS NULL OR s.IsPinned = @IsPinned)
    ORDER BY s.IsPinned DESC, s.CreatedDate DESC;
END;
GO
