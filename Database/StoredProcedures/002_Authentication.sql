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

IF COL_LENGTH(N'dbo.tblAI_Users', N'FullName') IS NULL
    ALTER TABLE dbo.tblAI_Users ADD FullName NVARCHAR(200) NULL;
GO

IF COL_LENGTH(N'dbo.tblAI_Users', N'MobileNumber') IS NULL
    ALTER TABLE dbo.tblAI_Users ADD MobileNumber NVARCHAR(30) NULL;
GO

IF OBJECT_ID(N'dbo.tblAI_LoginHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_LoginHistory
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_LoginHistory PRIMARY KEY CLUSTERED,
        UserId          INT NULL,
        Email           NVARCHAR(256) NOT NULL,
        IpAddress       NVARCHAR(64) NULL,
        UserAgent       NVARCHAR(512) NULL,
        IsSuccess       BIT NOT NULL,
        FailureReason   NVARCHAR(500) NULL,
        CreatedBy       INT NULL,
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_LoginHistory_CreatedDate DEFAULT SYSUTCDATETIME(),
        ModifiedBy      INT NULL,
        ModifiedDate    DATETIME2(3) NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_tblAI_LoginHistory_IsActive DEFAULT (1),
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblAI_LoginHistory_IsDeleted DEFAULT (0),
        CONSTRAINT FK_tblAI_LoginHistory_tblAI_Users FOREIGN KEY (UserId) REFERENCES dbo.tblAI_Users(Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.tblAI_RefreshTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblAI_RefreshTokens
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tblAI_RefreshTokens PRIMARY KEY CLUSTERED,
        UserId          INT NOT NULL,
        TokenHash       NVARCHAR(500) NOT NULL,
        ExpiresAtUtc    DATETIME2(3) NOT NULL,
        RevokedAtUtc    DATETIME2(3) NULL,
        ReplacedByTokenHash NVARCHAR(500) NULL,
        CreatedBy       INT NULL,
        CreatedDate     DATETIME2(3) NOT NULL CONSTRAINT DF_tblAI_RefreshTokens_CreatedDate DEFAULT SYSUTCDATETIME(),
        ModifiedBy      INT NULL,
        ModifiedDate    DATETIME2(3) NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_tblAI_RefreshTokens_IsActive DEFAULT (1),
        IsDeleted       BIT NOT NULL CONSTRAINT DF_tblAI_RefreshTokens_IsDeleted DEFAULT (0),
        CONSTRAINT FK_tblAI_RefreshTokens_tblAI_Users FOREIGN KEY (UserId) REFERENCES dbo.tblAI_Users(Id)
    );
END;
GO

DROP INDEX IF EXISTS IX_tblAI_LoginHistory_UserId ON dbo.tblAI_LoginHistory;
CREATE NONCLUSTERED INDEX IX_tblAI_LoginHistory_UserId
ON dbo.tblAI_LoginHistory (UserId, CreatedDate DESC)
INCLUDE (Email, IsSuccess);
GO

DROP INDEX IF EXISTS IX_tblAI_RefreshTokens_UserId ON dbo.tblAI_RefreshTokens;
CREATE NONCLUSTERED INDEX IX_tblAI_RefreshTokens_UserId
ON dbo.tblAI_RefreshTokens (UserId, ExpiresAtUtc DESC)
INCLUDE (TokenHash, IsActive, IsDeleted, RevokedAtUtc);
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_RegisterUser
    @FullName NVARCHAR(200),
    @Email NVARCHAR(256),
    @MobileNumber NVARCHAR(30),
    @PasswordHash NVARCHAR(500),
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tblAI_Users WHERE Email = @Email AND IsDeleted = 0)
    BEGIN
        SELECT CAST(0 AS INT) AS Id, CAST(1 AS BIT) AS IsDuplicateEmail;
        RETURN;
    END;

    INSERT INTO dbo.tblAI_Users (UserName, Email, PasswordHash, DisplayName, FullName, MobileNumber, CreatedBy)
    VALUES (@Email, @Email, @PasswordHash, @FullName, @FullName, @MobileNumber, @CreatedBy);

    DECLARE @UserId INT = CAST(SCOPE_IDENTITY() AS INT);
    DECLARE @RoleId INT;

    SELECT TOP (1) @RoleId = Id FROM dbo.tblAI_Roles WHERE RoleName = N'User' AND IsDeleted = 0;

    IF @RoleId IS NOT NULL
    BEGIN
        INSERT INTO dbo.tblAI_UserRoles (UserId, RoleId, CreatedBy)
        VALUES (@UserId, @RoleId, @CreatedBy);
    END;

    SELECT @UserId AS Id, CAST(0 AS BIT) AS IsDuplicateEmail;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_LoginUser
    @Email NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        Email,
        UserName,
        COALESCE(FullName, DisplayName, UserName) AS FullName,
        MobileNumber,
        PasswordHash,
        IsActive
    FROM dbo.tblAI_Users
    WHERE Email = @Email
      AND IsDeleted = 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_ValidateUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.Id,
        u.Email,
        u.UserName,
        COALESCE(u.FullName, u.DisplayName, u.UserName) AS FullName,
        u.MobileNumber,
        u.IsActive,
        COALESCE(roles.Roles, N'') AS Roles,
        COALESCE(permissions.Permissions, N'') AS Permissions
    FROM dbo.tblAI_Users u
    OUTER APPLY
    (
        SELECT STRING_AGG(roleNames.RoleName, N',') AS Roles
        FROM
        (
            SELECT DISTINCT r.RoleName
            FROM dbo.tblAI_UserRoles ur
            INNER JOIN dbo.tblAI_Roles r ON r.Id = ur.RoleId
            WHERE ur.UserId = u.Id
              AND ur.IsDeleted = 0
              AND ur.IsActive = 1
              AND r.IsDeleted = 0
              AND r.IsActive = 1
        ) roleNames
    ) roles
    OUTER APPLY
    (
        SELECT STRING_AGG(permissionNames.PermissionName, N',') AS Permissions
        FROM
        (
            SELECT DISTINCT p.PermissionName
            FROM dbo.tblAI_UserRoles ur
            INNER JOIN dbo.tblAI_Roles r ON r.Id = ur.RoleId
            INNER JOIN dbo.tblAI_RolePermissions rp ON rp.RoleId = r.Id
            INNER JOIN dbo.tblAI_Permissions p ON p.Id = rp.PermissionId
            WHERE ur.UserId = u.Id
              AND ur.IsDeleted = 0
              AND ur.IsActive = 1
              AND r.IsDeleted = 0
              AND r.IsActive = 1
              AND rp.IsDeleted = 0
              AND rp.IsActive = 1
              AND p.IsDeleted = 0
              AND p.IsActive = 1
        ) permissionNames
    ) permissions
    WHERE u.Id = @UserId
      AND u.IsDeleted = 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_SaveRefreshToken
    @UserId INT,
    @TokenHash NVARCHAR(500),
    @ExpiresAtUtc DATETIME2(3),
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tblAI_RefreshTokens (UserId, TokenHash, ExpiresAtUtc, CreatedBy)
    VALUES (@UserId, @TokenHash, @ExpiresAtUtc, @CreatedBy);

    SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_GetRefreshToken
    @TokenHash NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        Id,
        UserId,
        TokenHash,
        ExpiresAtUtc,
        RevokedAtUtc,
        IsActive,
        IsDeleted
    FROM dbo.tblAI_RefreshTokens
    WHERE TokenHash = @TokenHash
    ORDER BY Id DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_RevokeRefreshToken
    @TokenHash NVARCHAR(500),
    @ReplacedByTokenHash NVARCHAR(500) = NULL,
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblAI_RefreshTokens
       SET IsActive = 0,
           RevokedAtUtc = SYSUTCDATETIME(),
           ReplacedByTokenHash = @ReplacedByTokenHash,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE TokenHash = @TokenHash
      AND IsDeleted = 0;

    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_UpdateLastLogin
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblAI_Users
       SET LastLoginDate = SYSUTCDATETIME(),
           ModifiedDate = SYSUTCDATETIME(),
           ModifiedBy = @UserId
    WHERE Id = @UserId
      AND IsDeleted = 0;

    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_LogLoginHistory
    @UserId INT = NULL,
    @Email NVARCHAR(256),
    @IpAddress NVARCHAR(64) = NULL,
    @UserAgent NVARCHAR(512) = NULL,
    @IsSuccess BIT,
    @FailureReason NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tblAI_LoginHistory (UserId, Email, IpAddress, UserAgent, IsSuccess, FailureReason, CreatedBy)
    VALUES (@UserId, @Email, @IpAddress, @UserAgent, @IsSuccess, @FailureReason, @UserId);

    SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_GetRegisteredUserCount
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(1) AS RegisteredUserCount
    FROM dbo.tblAI_Users
    WHERE IsDeleted = 0
      AND IsActive = 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_GetUsersForAssistant
    @IsActive BIT = NULL,
    @RegisteredTodayOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id AS UserId,
        UserName,
        Email,
        COALESCE(FullName, DisplayName, UserName) AS FullName,
        IsActive,
        CreatedDate
    FROM dbo.tblAI_Users
    WHERE IsDeleted = 0
      AND (@IsActive IS NULL OR IsActive = @IsActive)
      AND
      (
          @RegisteredTodayOnly = 0
          OR CONVERT(date, CreatedDate) = CONVERT(date, SYSUTCDATETIME())
      )
    ORDER BY CreatedDate DESC, Id DESC;
END;
GO
