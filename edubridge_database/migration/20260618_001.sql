USE EduBridgeDB;
GO
-- =============================================
-- IMPACT ANALYSIS:
-- 1. Creates `Projects` table.
-- 2. Creates `Branches` table.
-- 3. Creates `SystemAuditLogs` table.
-- 4. Alters `Centers` to add `ProjectId` (FK).
-- 5. Alters `Rooms`, `Classes`, `Teachers`, `Students` to add `BranchId` (NULL).
-- 6. Seeds roles: SYSTEM_ADMIN, PROJECT_ADMIN, BRANCH_MANAGER.
-- 7. Migrates existing `Centers` by creating a default "Default Project" and a default "Main Branch" for each Center to maintain data integrity.
-- =============================================

BEGIN TRANSACTION;

-- 1. Create Projects Table
IF OBJECT_ID(N'[dbo].[Projects]', N'U') IS NULL
BEGIN
    CREATE TABLE Projects (
        ProjectId INT IDENTITY(1,1) NOT NULL,
        ProjectCode NVARCHAR(50) NOT NULL UNIQUE,
        ProjectName NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        CanCreateCenters BIT NOT NULL DEFAULT 1,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Active',
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT PK_Projects PRIMARY KEY CLUSTERED (ProjectId)
    );

    -- 2. Insert Default Project (only if we just created the table)
    INSERT INTO Projects (ProjectCode, ProjectName, Description, CanCreateCenters)
    VALUES ('DEFAULT_PROJ', 'Default Project', 'System generated default project for existing centers', 1);
END

DECLARE @DefaultProjectId INT;
SELECT @DefaultProjectId = ProjectId FROM Projects WHERE ProjectCode = 'DEFAULT_PROJ';

IF @DefaultProjectId IS NULL
BEGIN
    -- Fallback in case table exists but default project doesn't
    INSERT INTO Projects (ProjectCode, ProjectName, Description, CanCreateCenters)
    VALUES ('DEFAULT_PROJ', 'Default Project', 'System generated default project for existing centers', 1);
    SET @DefaultProjectId = SCOPE_IDENTITY();
END

-- 3. Alter Centers Table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Centers]') AND name = 'ProjectId')
BEGIN
    ALTER TABLE Centers ADD ProjectId INT NULL;
    ALTER TABLE Centers ADD CONSTRAINT FK_Centers_Projects FOREIGN KEY (ProjectId) REFERENCES Projects(ProjectId);

    -- Update existing Centers to use Default Project
    DECLARE @SqlUpdate NVARCHAR(MAX) = N'UPDATE Centers SET ProjectId = @DefaultProjId';
    EXEC sp_executesql @SqlUpdate, N'@DefaultProjId INT', @DefaultProjId = @DefaultProjectId;

    -- Now make ProjectId NOT NULL
    DECLARE @SqlAlter NVARCHAR(MAX) = N'ALTER TABLE Centers ALTER COLUMN ProjectId INT NOT NULL';
    EXEC sp_executesql @SqlAlter;
END

-- 4. Create Branches Table
IF OBJECT_ID(N'[dbo].[Branches]', N'U') IS NULL
BEGIN
    CREATE TABLE Branches (
        BranchId INT IDENTITY(1,1) NOT NULL,
        CenterId INT NOT NULL,
        BranchCode NVARCHAR(50) NOT NULL,
        BranchName NVARCHAR(150) NOT NULL,
        Address NVARCHAR(255) NULL,
        PhoneNumber NVARCHAR(20) NULL,
        Email NVARCHAR(150) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Active',
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT PK_Branches PRIMARY KEY CLUSTERED (BranchId),
        CONSTRAINT FK_Branches_Centers FOREIGN KEY (CenterId) REFERENCES Centers(CenterId),
        CONSTRAINT UQ_Branches_Code_Center UNIQUE (CenterId, BranchCode)
    );

    -- 5. Create Default Branches for Existing Centers
    INSERT INTO Branches (CenterId, BranchCode, BranchName, Address, PhoneNumber, Email)
    SELECT CenterId, 'MAIN_BRANCH', N'Cơ sở chính', Address, PhoneNumber, Email 
    FROM Centers;
END

-- 6. Add BranchId to business tables (Nullable)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Rooms]') AND name = 'BranchId')
BEGIN
    ALTER TABLE Rooms ADD BranchId INT NULL;
    ALTER TABLE Rooms ADD CONSTRAINT FK_Rooms_Branches FOREIGN KEY (BranchId) REFERENCES Branches(BranchId);
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Classes]') AND name = 'BranchId')
BEGIN
    ALTER TABLE Classes ADD BranchId INT NULL;
    ALTER TABLE Classes ADD CONSTRAINT FK_Classes_Branches FOREIGN KEY (BranchId) REFERENCES Branches(BranchId);
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Teachers]') AND name = 'BranchId')
BEGIN
    ALTER TABLE Teachers ADD BranchId INT NULL;
    ALTER TABLE Teachers ADD CONSTRAINT FK_Teachers_Branches FOREIGN KEY (BranchId) REFERENCES Branches(BranchId);
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Students]') AND name = 'BranchId')
BEGIN
    ALTER TABLE Students ADD BranchId INT NULL;
    ALTER TABLE Students ADD CONSTRAINT FK_Students_Branches FOREIGN KEY (BranchId) REFERENCES Branches(BranchId);
END

-- 7. Create SystemAuditLogs Table
IF OBJECT_ID(N'[dbo].[SystemAuditLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE SystemAuditLogs (
        LogId INT IDENTITY(1,1) NOT NULL,
        ActorUserId INT NOT NULL,
        TargetCenterId INT NULL,
        TargetProjectId INT NULL,
        Action NVARCHAR(50) NOT NULL,
        EntityName NVARCHAR(100) NOT NULL,
        EntityId NVARCHAR(50) NOT NULL,
        OldValue NVARCHAR(MAX) NULL,
        NewValue NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        IpAddress NVARCHAR(50) NULL,
        CONSTRAINT PK_SystemAuditLogs PRIMARY KEY CLUSTERED (LogId),
        CONSTRAINT FK_SystemAuditLogs_Users FOREIGN KEY (ActorUserId) REFERENCES Users(UserId)
    );
END

-- 8. Seed Roles
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleCode = 'SYSTEM_ADMIN')
BEGIN
    INSERT INTO Roles (RoleCode, RoleName) VALUES ('SYSTEM_ADMIN', N'System Administrator');
END

IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleCode = 'PROJECT_ADMIN')
BEGIN
    INSERT INTO Roles (RoleCode, RoleName) VALUES ('PROJECT_ADMIN', N'Project Administrator');
END

IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleCode = 'BRANCH_MANAGER')
BEGIN
    INSERT INTO Roles (RoleCode, RoleName) VALUES ('BRANCH_MANAGER', N'Branch Manager');
END

COMMIT TRANSACTION;

UPDATE Users
SET PasswordHash = '$2a$11$4AS8zQyRXz.pz14rBox1TekJ5O2z4uSzRVPaPbtogeEi2bZ3xnCAC'
WHERE Email IN (
    'admin@edubridge.com'
);

-- Thay đổi cấu trúc cột thành NULL
ALTER TABLE Centers
ALTER COLUMN OwnerUserId INT NULL;
