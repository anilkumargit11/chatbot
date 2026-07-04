SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE Ajay_DB;
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
        CONSTRAINT UQ_tblAI_Permissions_PermissionName UNIQUE NONCLUSTERED (PermissionName)
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
        CONSTRAINT FK_tblAI_RolePermissions_tblAI_Roles FOREIGN KEY (RoleId) REFERENCES dbo.tblAI_Roles(Id),
        CONSTRAINT FK_tblAI_RolePermissions_tblAI_Permissions FOREIGN KEY (PermissionId) REFERENCES dbo.tblAI_Permissions(Id),
        CONSTRAINT UQ_tblAI_RolePermissions_RoleId_PermissionId UNIQUE NONCLUSTERED (RoleId, PermissionId)
    );
END;
GO

DECLARE @Permissions TABLE (PermissionName NVARCHAR(100), Description NVARCHAR(500));
INSERT INTO @Permissions (PermissionName, Description)
VALUES
    (N'Dashboard.View', N'View dashboard metrics'),
    (N'ChatAssistant.View', N'Use chat assistant'),
    (N'Document.Upload', N'Upload documents'),
    (N'Document.Delete', N'Delete documents'),
    (N'ChatHistory.View', N'View chat history'),
    (N'KnowledgeBase.View', N'View knowledge base'),
    (N'Users.View', N'View users'),
    (N'Users.Create', N'Create users'),
    (N'Users.Edit', N'Edit users'),
    (N'Users.Delete', N'Delete users'),
    (N'Roles.View', N'View roles'),
    (N'Roles.Edit', N'Edit roles'),
    (N'Settings.View', N'View settings');

INSERT INTO dbo.tblAI_Permissions (PermissionName, Description)
SELECT p.PermissionName, p.Description
FROM @Permissions p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tblAI_Permissions existing
    WHERE existing.PermissionName = p.PermissionName
      AND existing.IsDeleted = 0
);
GO

UPDATE dbo.tblAI_Roles
   SET IsSystemRole = 1,
       IsActive = 1
WHERE RoleName IN (N'Administrator', N'User')
  AND IsDeleted = 0;
GO

INSERT INTO dbo.tblAI_RolePermissions (RoleId, PermissionId, CreatedBy)
SELECT r.Id, p.Id, NULL
FROM dbo.tblAI_Roles r
CROSS JOIN dbo.tblAI_Permissions p
WHERE r.RoleName = N'Administrator'
  AND r.IsDeleted = 0
  AND p.IsDeleted = 0
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.tblAI_RolePermissions rp
      WHERE rp.RoleId = r.Id
        AND rp.PermissionId = p.Id
  );
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_Admin_GetUsers
    @Search NVARCHAR(200) = NULL,
    @RoleId INT = NULL,
    @IsActive BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.Id,
        u.UserName,
        COALESCE(u.FullName, u.DisplayName, u.UserName) AS FullName,
        u.Email,
        COALESCE(u.MobileNumber, N'') AS MobileNumber,
        r.Id AS RoleId,
        COALESCE(r.RoleName, N'') AS RoleName,
        u.IsActive,
        u.CreatedDate,
        u.LastLoginDate
    FROM dbo.tblAI_Users u
    OUTER APPLY
    (
        SELECT TOP (1) roleTable.Id, roleTable.RoleName
        FROM dbo.tblAI_UserRoles userRole
        INNER JOIN dbo.tblAI_Roles roleTable ON roleTable.Id = userRole.RoleId
        WHERE userRole.UserId = u.Id
          AND userRole.IsDeleted = 0
          AND userRole.IsActive = 1
          AND roleTable.IsDeleted = 0
        ORDER BY roleTable.RoleName
    ) r
    WHERE u.IsDeleted = 0
      AND (@IsActive IS NULL OR u.IsActive = @IsActive)
      AND (@RoleId IS NULL OR r.Id = @RoleId)
      AND (
          @Search IS NULL OR @Search = N'' OR
          u.UserName LIKE N'%' + @Search + N'%' OR
          u.Email LIKE N'%' + @Search + N'%' OR
          u.FullName LIKE N'%' + @Search + N'%'
      )
    ORDER BY u.CreatedDate DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_Admin_GetUserById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.Id,
        u.UserName,
        COALESCE(u.FullName, u.DisplayName, u.UserName) AS FullName,
        u.Email,
        COALESCE(u.MobileNumber, N'') AS MobileNumber,
        r.Id AS RoleId,
        COALESCE(r.RoleName, N'') AS RoleName,
        u.IsActive,
        u.CreatedDate,
        u.LastLoginDate
    FROM dbo.tblAI_Users u
    OUTER APPLY
    (
        SELECT TOP (1) roleTable.Id, roleTable.RoleName
        FROM dbo.tblAI_UserRoles userRole
        INNER JOIN dbo.tblAI_Roles roleTable ON roleTable.Id = userRole.RoleId
        WHERE userRole.UserId = u.Id
          AND userRole.IsDeleted = 0
          AND userRole.IsActive = 1
          AND roleTable.IsDeleted = 0
        ORDER BY roleTable.RoleName
    ) r
    WHERE u.Id = @Id
      AND u.IsDeleted = 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_Admin_CreateUser
    @UserName NVARCHAR(100),
    @FullName NVARCHAR(200),
    @Email NVARCHAR(256),
    @MobileNumber NVARCHAR(30) = NULL,
    @PasswordHash NVARCHAR(500),
    @RoleId INT,
    @IsActive BIT,
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tblAI_Users WHERE IsDeleted = 0 AND (UserName = @UserName OR Email = @Email))
    BEGIN
        SELECT CAST(0 AS INT);
        RETURN;
    END;

    INSERT INTO dbo.tblAI_Users (UserName, Email, PasswordHash, DisplayName, FullName, MobileNumber, IsActive, CreatedBy)
    VALUES (@UserName, @Email, @PasswordHash, @FullName, @FullName, @MobileNumber, @IsActive, @CreatedBy);

    DECLARE @UserId INT = CAST(SCOPE_IDENTITY() AS INT);

    INSERT INTO dbo.tblAI_UserRoles (UserId, RoleId, CreatedBy)
    VALUES (@UserId, @RoleId, @CreatedBy);

    SELECT @UserId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_Admin_UpdateUser
    @Id INT,
    @UserName NVARCHAR(100),
    @FullName NVARCHAR(200),
    @Email NVARCHAR(256),
    @MobileNumber NVARCHAR(30) = NULL,
    @RoleId INT,
    @IsActive BIT,
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tblAI_Users WHERE Id <> @Id AND IsDeleted = 0 AND (UserName = @UserName OR Email = @Email))
    BEGIN
        SELECT CAST(0 AS INT);
        RETURN;
    END;

    UPDATE dbo.tblAI_Users
       SET UserName = @UserName,
           Email = @Email,
           DisplayName = @FullName,
           FullName = @FullName,
           MobileNumber = @MobileNumber,
           IsActive = @IsActive,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @Id
      AND IsDeleted = 0;

    IF @@ROWCOUNT = 0
    BEGIN
        SELECT CAST(0 AS INT);
        RETURN;
    END;

    UPDATE dbo.tblAI_UserRoles
       SET IsDeleted = 1,
           IsActive = 0,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE UserId = @Id
      AND IsDeleted = 0;

    IF EXISTS (SELECT 1 FROM dbo.tblAI_UserRoles WHERE UserId = @Id AND RoleId = @RoleId)
    BEGIN
        UPDATE dbo.tblAI_UserRoles
           SET IsDeleted = 0,
               IsActive = 1,
               ModifiedBy = @ModifiedBy,
               ModifiedDate = SYSUTCDATETIME()
        WHERE UserId = @Id
          AND RoleId = @RoleId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.tblAI_UserRoles (UserId, RoleId, CreatedBy)
        VALUES (@Id, @RoleId, @ModifiedBy);
    END;

    SELECT CAST(1 AS INT);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_Admin_DeleteUser
    @Id INT,
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblAI_Users
       SET IsDeleted = 1,
           IsActive = 0,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @Id
      AND IsDeleted = 0;

    DECLARE @RowsAffected INT = @@ROWCOUNT;

    IF @RowsAffected > 0
    BEGIN
        UPDATE dbo.tblAI_UserRoles
           SET IsDeleted = 1,
               IsActive = 0,
               ModifiedBy = @ModifiedBy,
               ModifiedDate = SYSUTCDATETIME()
        WHERE UserId = @Id
          AND IsDeleted = 0;
    END;

    SELECT @RowsAffected;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_Admin_SetUserStatus
    @Id INT,
    @IsActive BIT,
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblAI_Users
       SET IsActive = @IsActive,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @Id
      AND IsDeleted = 0;

    SELECT @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_Admin_ResetUserPassword
    @Id INT,
    @PasswordHash NVARCHAR(500),
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblAI_Users
       SET PasswordHash = @PasswordHash,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @Id
      AND IsDeleted = 0;

    SELECT @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_Admin_GetRoles
    @Search NVARCHAR(200) = NULL,
    @IsActive BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, RoleName, COALESCE(Description, N'') AS Description, IsSystemRole, IsActive, CreatedDate
    FROM dbo.tblAI_Roles
    WHERE IsDeleted = 0
      AND (@IsActive IS NULL OR IsActive = @IsActive)
      AND (@Search IS NULL OR @Search = N'' OR RoleName LIKE N'%' + @Search + N'%' OR Description LIKE N'%' + @Search + N'%')
    ORDER BY IsSystemRole DESC, RoleName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_Admin_GetRoleById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, RoleName, COALESCE(Description, N'') AS Description, IsSystemRole, IsActive, CreatedDate
    FROM dbo.tblAI_Roles
    WHERE Id = @Id
      AND IsDeleted = 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_Admin_CreateRole
    @RoleName NVARCHAR(100),
    @Description NVARCHAR(500) = NULL,
    @IsActive BIT,
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tblAI_Roles WHERE RoleName = @RoleName AND IsDeleted = 0)
    BEGIN
        SELECT CAST(0 AS INT);
        RETURN;
    END;

    INSERT INTO dbo.tblAI_Roles (RoleName, Description, IsActive, CreatedBy)
    VALUES (@RoleName, @Description, @IsActive, @CreatedBy);

    SELECT CAST(SCOPE_IDENTITY() AS INT);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_Admin_UpdateRole
    @Id INT,
    @RoleName NVARCHAR(100),
    @Description NVARCHAR(500) = NULL,
    @IsActive BIT,
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tblAI_Roles WHERE Id <> @Id AND RoleName = @RoleName AND IsDeleted = 0)
    BEGIN
        SELECT CAST(0 AS INT);
        RETURN;
    END;

    UPDATE dbo.tblAI_Roles
       SET RoleName = @RoleName,
           Description = @Description,
           IsActive = @IsActive,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @Id
      AND IsDeleted = 0;

    SELECT @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_Admin_DeleteRole
    @Id INT,
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.tblAI_Roles WHERE Id = @Id AND IsSystemRole = 1)
       OR EXISTS (SELECT 1 FROM dbo.tblAI_UserRoles WHERE RoleId = @Id AND IsDeleted = 0 AND IsActive = 1)
    BEGIN
        SELECT CAST(0 AS INT);
        RETURN;
    END;

    UPDATE dbo.tblAI_Roles
       SET IsDeleted = 1,
           IsActive = 0,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @Id
      AND IsDeleted = 0;

    SELECT @@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_Admin_GetPermissions
    @RoleId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.Id,
        p.PermissionName,
        COALESCE(p.Description, N'') AS Description,
        CAST(CASE WHEN rp.Id IS NULL THEN 0 ELSE 1 END AS BIT) AS IsAssigned,
        p.IsActive
    FROM dbo.tblAI_Permissions p
    LEFT JOIN dbo.tblAI_RolePermissions rp
        ON rp.PermissionId = p.Id
       AND rp.RoleId = @RoleId
       AND rp.IsDeleted = 0
       AND rp.IsActive = 1
    WHERE p.IsDeleted = 0
    ORDER BY p.PermissionName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AI_Admin_AssignRolePermissions
    @RoleId INT,
    @PermissionIds NVARCHAR(MAX),
    @ModifiedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.tblAI_Roles WHERE Id = @RoleId AND IsDeleted = 0)
    BEGIN
        SELECT CAST(0 AS INT);
        RETURN;
    END;

    UPDATE dbo.tblAI_RolePermissions
       SET IsDeleted = 1,
           IsActive = 0,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    WHERE RoleId = @RoleId
      AND IsDeleted = 0;

    INSERT INTO dbo.tblAI_RolePermissions (RoleId, PermissionId, CreatedBy)
    SELECT @RoleId, TRY_CAST(value AS INT), @ModifiedBy
    FROM STRING_SPLIT(COALESCE(@PermissionIds, N''), N',')
    WHERE TRY_CAST(value AS INT) IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.tblAI_RolePermissions existing
          WHERE existing.RoleId = @RoleId
            AND existing.PermissionId = TRY_CAST(value AS INT)
      );

    UPDATE existing
       SET IsDeleted = 0,
           IsActive = 1,
           ModifiedBy = @ModifiedBy,
           ModifiedDate = SYSUTCDATETIME()
    FROM dbo.tblAI_RolePermissions existing
    INNER JOIN STRING_SPLIT(COALESCE(@PermissionIds, N''), N',') splitIds
        ON existing.PermissionId = TRY_CAST(splitIds.value AS INT)
    WHERE existing.RoleId = @RoleId;

    SELECT CAST(1 AS INT);
END;
GO
