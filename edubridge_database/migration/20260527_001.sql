USE EduBridgeDB;
GO

/* =========================================================
   1. Đảm bảo role PARENT tồn tại
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM dbo.Roles
    WHERE RoleCode = N'PARENT'
)
BEGIN
    INSERT INTO dbo.Roles (RoleCode, RoleName)
    VALUES (N'PARENT', N'Phụ huynh');
END;
GO


/* =========================================================
   2. Bổ sung thông tin liên hệ riêng của học sinh
   ========================================================= */

IF COL_LENGTH(N'dbo.Students', N'PhoneNumber') IS NULL
BEGIN
    ALTER TABLE dbo.Students
    ADD PhoneNumber NVARCHAR(20) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Students', N'Email') IS NULL
BEGIN
    ALTER TABLE dbo.Students
    ADD Email NVARCHAR(150) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Students', N'NormalizedPhoneNumber') IS NULL
BEGIN
    ALTER TABLE dbo.Students
    ADD NormalizedPhoneNumber NVARCHAR(20) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Students', N'AvatarUrl') IS NULL
BEGIN
    ALTER TABLE dbo.Students
    ADD AvatarUrl NVARCHAR(500) NULL;
END;
GO


/* =========================================================
   3. Chuẩn hóa dữ liệu liên hệ học sinh hiện có
   ========================================================= */

UPDATE dbo.Students
SET Email = NULL
WHERE Email IS NOT NULL
  AND LTRIM(RTRIM(Email)) = N'';
GO

UPDATE dbo.Students
SET Email = LOWER(LTRIM(RTRIM(Email)))
WHERE Email IS NOT NULL;
GO

UPDATE dbo.Students
SET NormalizedPhoneNumber =
    CASE
        WHEN PhoneNumber IS NULL THEN NULL
        WHEN LEFT(
            REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(PhoneNumber, N' ', N''), N'-', N''), N'.', N''), N'(', N''), N')', N''), N'+', N''),
            2
        ) = N'84'
        THEN N'0' + SUBSTRING(
            REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(PhoneNumber, N' ', N''), N'-', N''), N'.', N''), N'(', N''), N')', N''), N'+', N''),
            3,
            20
        )
        ELSE REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(PhoneNumber, N' ', N''), N'-', N''), N'.', N''), N'(', N''), N')', N''), N'+', N'')
    END
WHERE PhoneNumber IS NOT NULL;
GO


/* =========================================================
   4. Index phục vụ search/list học sinh nhanh
   ========================================================= */

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Students')
      AND name = N'IX_Students_CenterId_Status_StudentId'
)
BEGIN
    CREATE INDEX IX_Students_CenterId_Status_StudentId
    ON dbo.Students(CenterId, Status, StudentId DESC);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Students')
      AND name = N'IX_Students_CenterId_FullName'
)
BEGIN
    CREATE INDEX IX_Students_CenterId_FullName
    ON dbo.Students(CenterId, FullName);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Students')
      AND name = N'IX_Students_CenterId_StudentCode'
)
BEGIN
    CREATE INDEX IX_Students_CenterId_StudentCode
    ON dbo.Students(CenterId, StudentCode);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Students')
      AND name = N'IX_Students_NormalizedPhoneNumber'
)
BEGIN
    CREATE INDEX IX_Students_NormalizedPhoneNumber
    ON dbo.Students(NormalizedPhoneNumber)
    WHERE NormalizedPhoneNumber IS NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Students')
      AND name = N'IX_Students_Email'
)
BEGIN
    CREATE INDEX IX_Students_Email
    ON dbo.Students(Email)
    WHERE Email IS NOT NULL;
END;
GO


/* =========================================================
   5. Chuẩn hóa status học sinh
   Backend nên dùng Active / Inactive
   ========================================================= */

UPDATE dbo.Students
SET Status = N'Active'
WHERE Status IN (N'Đang học', N'Dang hoc', N'đang học');
GO

UPDATE dbo.Students
SET Status = N'Inactive'
WHERE Status IN (N'Nghỉ học', N'Nghi hoc', N'Đã nghỉ', N'Da nghi');
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Students_Status'
      AND parent_object_id = OBJECT_ID(N'dbo.Students')
)
BEGIN
    ALTER TABLE dbo.Students
    ADD CONSTRAINT CK_Students_Status
    CHECK (Status IN (N'Active', N'Inactive'));
END;
GO


/* =========================================================
   6. Recreate vw_StudentOverview mở rộng
   ========================================================= */

IF OBJECT_ID(N'dbo.vw_StudentOverview', N'V') IS NOT NULL
BEGIN
    DROP VIEW dbo.vw_StudentOverview;
END;
GO

CREATE VIEW dbo.vw_StudentOverview AS
SELECT
    s.StudentId,
    s.StudentCode,
    s.FullName AS StudentName,
    s.DateOfBirth,
    s.Gender,
    s.PhoneNumber AS StudentPhone,
    s.Email AS StudentEmail,
    s.AvatarUrl,
    s.CenterId,
    c.CenterName,
    s.Status,

    p.UserId AS ParentUserId,
    p.FullName AS ParentName,
    p.PhoneNumber AS ParentPhone,
    p.Email AS ParentEmail,

    cls.ClassId AS CurrentClassId,
    cls.ClassCode AS CurrentClassCode,
    cls.ClassName AS CurrentClassName,
    co.CourseName AS CurrentCourseName
FROM dbo.Students s
JOIN dbo.Centers c
    ON c.CenterId = s.CenterId
JOIN dbo.Users p
    ON p.UserId = s.ParentUserId
OUTER APPLY (
    SELECT TOP 1
        e.ClassId
    FROM dbo.Enrollments e
    WHERE e.StudentId = s.StudentId
      AND e.Status = N'Đang học'
    ORDER BY e.EnrollmentId DESC
) currentEnrollment
LEFT JOIN dbo.Classes cls
    ON cls.ClassId = currentEnrollment.ClassId
LEFT JOIN dbo.Courses co
    ON co.CourseId = cls.CourseId;
GO


/* =========================================================
   7. Kiểm tra nhanh
   ========================================================= */

SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = N'Students'
ORDER BY ORDINAL_POSITION;
GO

SELECT TOP 20
    StudentId,
    StudentCode,
    StudentName,
    StudentPhone,
    StudentEmail,
    ParentName,
    ParentPhone,
    ParentEmail,
    CurrentClassName,
    Status
FROM dbo.vw_StudentOverview
ORDER BY StudentId DESC;
GO

/*======Insert student======*/
USE EduBridgeDB;
GO

/* =========================================================
   Insert dữ liệu test: 1 học sinh đang học nhiều lớp
   Chỉ INSERT, không sửa/xóa dữ liệu cũ
   ========================================================= */

DECLARE @CenterId INT;
DECLARE @ParentRoleId INT;
DECLARE @ParentUserId INT;
DECLARE @StudentId INT;
DECLARE @CourseId1 INT;
DECLARE @CourseId2 INT;
DECLARE @TeacherId INT;
DECLARE @ClassId1 INT;
DECLARE @ClassId2 INT;

SELECT TOP 1 @CenterId = CenterId
FROM dbo.Centers
WHERE Status = N'Active'
ORDER BY CenterId;

SELECT TOP 1 @ParentRoleId = RoleId
FROM dbo.Roles
WHERE RoleCode = N'PARENT';

SELECT TOP 1 @TeacherId = TeacherId
FROM dbo.Teachers
WHERE CenterId = @CenterId
ORDER BY TeacherId;

SELECT TOP 1 @CourseId1 = CourseId
FROM dbo.Courses
WHERE CenterId = @CenterId
ORDER BY CourseId;

SELECT TOP 1 @CourseId2 = CourseId
FROM dbo.Courses
WHERE CenterId = @CenterId
  AND CourseId <> @CourseId1
ORDER BY CourseId;

IF @CourseId2 IS NULL
BEGIN
    SET @CourseId2 = @CourseId1;
END;

IF @CenterId IS NULL OR @ParentRoleId IS NULL OR @TeacherId IS NULL OR @CourseId1 IS NULL
BEGIN
    THROW 50001, N'Thiếu Center / Role PARENT / Teacher / Course để insert dữ liệu test.', 1;
END;


/* =========================================================
   1. Tạo phụ huynh test
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM dbo.Users
    WHERE Email = N'parent.multi.student@test.local'
)
BEGIN
    INSERT INTO dbo.Users
        (
            RoleId,
            FullName,
            Email,
            PasswordHash,
            PhoneNumber,
            NormalizedPhoneNumber,
            EmailConfirmed,
            Status,
            CreatedAt
        )
    VALUES
        (
            @ParentRoleId,
            N'Phụ huynh Test Nhiều Lớp',
            N'parent.multi.student@test.local',
            N'$2a$11$QG4CzAQhnBI8ZnqoRMzKtu7CFPFqSTnERLCTmc.fqBunXaUlyH/MK', -- 123456
            N'0911999001',
            N'0911999001',
            1,
            N'Active',
            SYSDATETIME()
        );
END;

SELECT @ParentUserId = UserId
FROM dbo.Users
WHERE Email = N'parent.multi.student@test.local';


/* =========================================================
   2. Tạo học sinh test
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM dbo.Students
    WHERE StudentCode = N'STD-MULTI-001'
)
BEGIN
    INSERT INTO dbo.Students
        (
            ParentUserId,
            CenterId,
            StudentCode,
            FullName,
            DateOfBirth,
            Gender,
            Address,
            PhoneNumber,
            Email,
            NormalizedPhoneNumber,
            Status,
            CreatedAt
        )
    VALUES
        (
            @ParentUserId,
            @CenterId,
            N'STD-MULTI-001',
            N'Học Sinh Test Nhiều Lớp',
            '2014-09-10',
            N'Nam',
            N'Địa chỉ test',
            N'0988999001',
            N'student.multi@test.local',
            N'0988999001',
            N'Active',
            SYSDATETIME()
        );
END;

SELECT @StudentId = StudentId
FROM dbo.Students
WHERE StudentCode = N'STD-MULTI-001';


/* =========================================================
   3. Tạo 2 lớp test
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM dbo.Classes
    WHERE ClassCode = N'CLS-MULTI-001'
)
BEGIN
    INSERT INTO dbo.Classes
        (
            CenterId,
            CourseId,
            TeacherId,
            ClassCode,
            ClassName,
            Room,
            ScheduleText,
            StartDate,
            EndDate,
            Status
        )
    VALUES
        (
            @CenterId,
            @CourseId1,
            @TeacherId,
            N'CLS-MULTI-001',
            N'Lớp Test Multi 01',
            N'Phòng Test 01',
            N'Thứ 2 08:00-10:00',
            CAST(GETDATE() AS DATE),
            DATEADD(MONTH, 3, CAST(GETDATE() AS DATE)),
            N'Active'
        );
END;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.Classes
    WHERE ClassCode = N'CLS-MULTI-002'
)
BEGIN
    INSERT INTO dbo.Classes
        (
            CenterId,
            CourseId,
            TeacherId,
            ClassCode,
            ClassName,
            Room,
            ScheduleText,
            StartDate,
            EndDate,
            Status
        )
    VALUES
        (
            @CenterId,
            @CourseId2,
            @TeacherId,
            N'CLS-MULTI-002',
            N'Lớp Test Multi 02',
            N'Phòng Test 02',
            N'Thứ 4 18:00-20:00',
            CAST(GETDATE() AS DATE),
            DATEADD(MONTH, 3, CAST(GETDATE() AS DATE)),
            N'Active'
        );
END;

SELECT @ClassId1 = ClassId
FROM dbo.Classes
WHERE ClassCode = N'CLS-MULTI-001';

SELECT @ClassId2 = ClassId
FROM dbo.Classes
WHERE ClassCode = N'CLS-MULTI-002';


/* =========================================================
   4. Ghi danh cùng 1 học sinh vào 2 lớp
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM dbo.Enrollments
    WHERE StudentId = @StudentId
      AND ClassId = @ClassId1
)
BEGIN
    INSERT INTO dbo.Enrollments
        (
            StudentId,
            ClassId,
            EnrollDate,
            Status
        )
    VALUES
        (
            @StudentId,
            @ClassId1,
            CAST(GETDATE() AS DATE),
            N'Đang học'
        );
END;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.Enrollments
    WHERE StudentId = @StudentId
      AND ClassId = @ClassId2
)
BEGIN
    INSERT INTO dbo.Enrollments
        (
            StudentId,
            ClassId,
            EnrollDate,
            Status
        )
    VALUES
        (
            @StudentId,
            @ClassId2,
            CAST(GETDATE() AS DATE),
            N'Đang học'
        );
END;


/* =========================================================
   5. Kiểm tra kết quả
   ========================================================= */

SELECT
    s.StudentId,
    s.StudentCode,
    s.FullName AS StudentName,
    p.FullName AS ParentName,
    p.PhoneNumber AS ParentPhone,
    c.ClassId,
    c.ClassCode,
    c.ClassName,
    e.Status AS EnrollmentStatus
FROM dbo.Students s
JOIN dbo.Users p
    ON p.UserId = s.ParentUserId
JOIN dbo.Enrollments e
    ON e.StudentId = s.StudentId
JOIN dbo.Classes c
    ON c.ClassId = e.ClassId
WHERE s.StudentCode = N'STD-MULTI-001'
ORDER BY c.ClassId;
GO


/*======Quản lý Room=====*/
USE EduBridgeDB;
GO

/* =========================================================
   1. Tạo bảng Rooms
   ========================================================= */

IF OBJECT_ID(N'dbo.Rooms', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Rooms (
        RoomId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CenterId INT NOT NULL,
        RoomCode NVARCHAR(30) NOT NULL,
        RoomName NVARCHAR(100) NOT NULL,
        Capacity INT NULL,
        Location NVARCHAR(150) NULL,
        Note NVARCHAR(255) NULL,
        Status NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Rooms_Status DEFAULT N'Active',
        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_Rooms_CreatedAt DEFAULT SYSDATETIME(),

        CONSTRAINT FK_Rooms_Centers
            FOREIGN KEY (CenterId) REFERENCES dbo.Centers(CenterId),

        CONSTRAINT CK_Rooms_Status
            CHECK (Status IN (N'Active', N'Inactive', N'Maintenance')),

        CONSTRAINT CK_Rooms_Capacity
            CHECK (Capacity IS NULL OR Capacity > 0)
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.Rooms')
      AND name = N'UQ_Rooms_Center_RoomCode'
)
BEGIN
    ALTER TABLE dbo.Rooms
    ADD CONSTRAINT UQ_Rooms_Center_RoomCode
    UNIQUE (CenterId, RoomCode);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Rooms')
      AND name = N'IX_Rooms_CenterId_Status'
)
BEGIN
    CREATE INDEX IX_Rooms_CenterId_Status
    ON dbo.Rooms(CenterId, Status);
END;
GO


/* =========================================================
   2. Seed Rooms từ dữ liệu Classes.Room hiện tại
   Rule:
   - RoomName lấy từ Classes.Room
   - RoomCode chuẩn hóa từ RoomName, ví dụ PHONG-201
   ========================================================= */

;WITH DistinctRooms AS
(
    SELECT DISTINCT
        c.CenterId,
        RoomName = LTRIM(RTRIM(c.Room))
    FROM dbo.Classes c
    WHERE c.Room IS NOT NULL
      AND LTRIM(RTRIM(c.Room)) <> N''
),
NormalizedRooms AS
(
    SELECT
        CenterId,
        RoomName,
        BaseRoomCode =
            UPPER(
                REPLACE(
                REPLACE(
                REPLACE(
                REPLACE(
                REPLACE(RoomName, N' ', N'-'),
                    N'Đ', N'D'),
                    N'đ', N'd'),
                    N'/', N'-'),
                    N'\', N'-')
            )
    FROM DistinctRooms
),
NumberedRooms AS
(
    SELECT
        CenterId,
        RoomName,
        RoomCode =
            LEFT(
                CASE
                    WHEN BaseRoomCode IS NULL OR BaseRoomCode = N'' THEN N'ROOM'
                    ELSE BaseRoomCode
                END,
                24
            ) + N'-' + RIGHT(N'0000' + CAST(
                ROW_NUMBER() OVER (
                    PARTITION BY CenterId,
                    LEFT(
                        CASE
                            WHEN BaseRoomCode IS NULL OR BaseRoomCode = N'' THEN N'ROOM'
                            ELSE BaseRoomCode
                        END,
                        24
                    )
                    ORDER BY RoomName
                ) AS NVARCHAR(10)), 4)
    FROM NormalizedRooms
)
INSERT INTO dbo.Rooms
    (CenterId, RoomCode, RoomName, Status)
SELECT
    nr.CenterId,
    nr.RoomCode,
    nr.RoomName,
    N'Active'
FROM NumberedRooms nr
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Rooms r
    WHERE r.CenterId = nr.CenterId
      AND r.RoomName = nr.RoomName
);
GO


/* =========================================================
   3. Thêm Classes.RoomId
   ========================================================= */

IF COL_LENGTH(N'dbo.Classes', N'RoomId') IS NULL
BEGIN
    ALTER TABLE dbo.Classes
    ADD RoomId INT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.Classes')
      AND name = N'FK_Classes_Rooms'
)
BEGIN
    ALTER TABLE dbo.Classes
    ADD CONSTRAINT FK_Classes_Rooms
    FOREIGN KEY (RoomId) REFERENCES dbo.Rooms(RoomId);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Classes')
      AND name = N'IX_Classes_RoomId'
)
BEGIN
    CREATE INDEX IX_Classes_RoomId
    ON dbo.Classes(RoomId);
END;
GO


/* =========================================================
   4. Map Classes.RoomId theo Room text hiện tại
   ========================================================= */

UPDATE c
SET RoomId = r.RoomId
FROM dbo.Classes c
JOIN dbo.Rooms r
    ON r.CenterId = c.CenterId
   AND r.RoomName = LTRIM(RTRIM(c.Room))
WHERE c.RoomId IS NULL
  AND c.Room IS NOT NULL
  AND LTRIM(RTRIM(c.Room)) <> N'';
GO


/* =========================================================
   5. View hỗ trợ quản lý phòng học
   ========================================================= */

IF OBJECT_ID(N'dbo.vw_RoomOverview', N'V') IS NOT NULL
BEGIN
    DROP VIEW dbo.vw_RoomOverview;
END;
GO

CREATE VIEW dbo.vw_RoomOverview AS
SELECT
    r.RoomId,
    r.CenterId,
    r.RoomCode,
    r.RoomName,
    r.Capacity,
    r.Location,
    r.Note,
    r.Status,
    COUNT(c.ClassId) AS TotalClasses,
    SUM(CASE WHEN c.Status = N'Active' THEN 1 ELSE 0 END) AS ActiveClasses
FROM dbo.Rooms r
LEFT JOIN dbo.Classes c
    ON c.RoomId = r.RoomId
GROUP BY
    r.RoomId,
    r.CenterId,
    r.RoomCode,
    r.RoomName,
    r.Capacity,
    r.Location,
    r.Note,
    r.Status;
GO


/* =========================================================
   6. Kiểm tra nhanh
   ========================================================= */

SELECT
    r.RoomId,
    r.CenterId,
    r.RoomCode,
    r.RoomName,
    r.Capacity,
    r.Location,
    r.Status,
    r.CreatedAt
FROM dbo.Rooms r
ORDER BY r.CenterId, r.RoomName;
GO

SELECT
    c.ClassId,
    c.ClassCode,
    c.ClassName,
    c.Room AS OldRoomText,
    c.RoomId,
    r.RoomCode,
    r.RoomName
FROM dbo.Classes c
LEFT JOIN dbo.Rooms r
    ON r.RoomId = c.RoomId
ORDER BY c.ClassId;
GO

SELECT *
FROM dbo.vw_RoomOverview
ORDER BY CenterId, RoomName;
GO



USE EduBridgeDB;
GO

/* Users: soft delete columns */
IF COL_LENGTH(N'dbo.Users', N'IsDeleted') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD IsDeleted BIT NOT NULL
        CONSTRAINT DF_Users_IsDeleted DEFAULT 0;
END;
GO

IF COL_LENGTH(N'dbo.Users', N'DeletedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD DeletedAt DATETIME2 NULL;
END;
GO

IF COL_LENGTH(N'dbo.Users', N'DeletedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD DeletedByUserId INT NULL;
END;
GO

/* Teachers: soft delete columns */
IF COL_LENGTH(N'dbo.Teachers', N'IsDeleted') IS NULL
BEGIN
    ALTER TABLE dbo.Teachers
    ADD IsDeleted BIT NOT NULL
        CONSTRAINT DF_Teachers_IsDeleted DEFAULT 0;
END;
GO

IF COL_LENGTH(N'dbo.Teachers', N'DeletedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Teachers
    ADD DeletedAt DATETIME2 NULL;
END;
GO

IF COL_LENGTH(N'dbo.Teachers', N'DeletedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Teachers
    ADD DeletedByUserId INT NULL;
END;
GO

/* Recreate filtered unique index: Teachers.TeacherCode */
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Teachers')
      AND name = N'UQ_Teachers_TeacherCode'
)
BEGIN
    DROP INDEX UQ_Teachers_TeacherCode ON dbo.Teachers;
END;
GO

CREATE UNIQUE INDEX UQ_Teachers_TeacherCode
ON dbo.Teachers(TeacherCode)
WHERE IsDeleted = 0;
GO

/* Recreate filtered unique index: Users.Email */
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Users')
      AND name = N'UX_Users_Email_NotNull'
)
BEGIN
    DROP INDEX UX_Users_Email_NotNull ON dbo.Users;
END;
GO

CREATE UNIQUE INDEX UX_Users_Email_NotNull
ON dbo.Users(Email)
WHERE Email IS NOT NULL
  AND IsDeleted = 0;
GO

/* Recreate filtered unique index: Users.NormalizedPhoneNumber */
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Users')
      AND name = N'UX_Users_NormalizedPhoneNumber_NotNull'
)
BEGIN
    DROP INDEX UX_Users_NormalizedPhoneNumber_NotNull ON dbo.Users;
END;
GO

CREATE UNIQUE INDEX UX_Users_NormalizedPhoneNumber_NotNull
ON dbo.Users(NormalizedPhoneNumber)
WHERE NormalizedPhoneNumber IS NOT NULL
  AND IsDeleted = 0;
GO

/* Recreate filtered unique index: Users.IdentityNumber */
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Users')
      AND name = N'UX_Users_IdentityNumber_NotNull'
)
BEGIN
    DROP INDEX UX_Users_IdentityNumber_NotNull ON dbo.Users;
END;
GO

CREATE UNIQUE INDEX UX_Users_IdentityNumber_NotNull
ON dbo.Users(IdentityNumber)
WHERE IdentityNumber IS NOT NULL
  AND IsDeleted = 0;
GO

/* Useful query indexes for soft delete */
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Teachers')
      AND name = N'IX_Teachers_CenterId_IsDeleted_Status'
)
BEGIN
    CREATE INDEX IX_Teachers_CenterId_IsDeleted_Status
    ON dbo.Teachers(CenterId, IsDeleted, Status);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Users')
      AND name = N'IX_Users_IsDeleted_Status_RoleId'
)
BEGIN
    CREATE INDEX IX_Users_IsDeleted_Status_RoleId
    ON dbo.Users(IsDeleted, Status, RoleId);
END;
GO

/* Verify */
SELECT
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN (N'Users', N'Teachers')
  AND COLUMN_NAME IN (N'IsDeleted', N'DeletedAt', N'DeletedByUserId')
ORDER BY TABLE_NAME, COLUMN_NAME;
GO

SELECT
    OBJECT_NAME(object_id) AS TableName,
    name AS IndexName,
    filter_definition
FROM sys.indexes
WHERE object_id IN (OBJECT_ID(N'dbo.Users'), OBJECT_ID(N'dbo.Teachers'))
  AND name IN (
      N'UQ_Teachers_TeacherCode',
      N'UX_Users_Email_NotNull',
      N'UX_Users_NormalizedPhoneNumber_NotNull',
      N'UX_Users_IdentityNumber_NotNull',
      N'IX_Teachers_CenterId_IsDeleted_Status',
      N'IX_Users_IsDeleted_Status_RoleId'
  )
ORDER BY TableName, IndexName;
GO


USE EduBridgeDB;
GO

DECLARE @ConstraintName SYSNAME;

SELECT @ConstraintName = kc.name
FROM sys.key_constraints kc
JOIN sys.index_columns ic
    ON ic.object_id = kc.parent_object_id
   AND ic.index_id = kc.unique_index_id
JOIN sys.columns c
    ON c.object_id = ic.object_id
   AND c.column_id = ic.column_id
WHERE kc.parent_object_id = OBJECT_ID(N'dbo.Teachers')
  AND kc.type = N'UQ'
  AND c.name = N'TeacherCode';

IF @ConstraintName IS NOT NULL
BEGIN
    DECLARE @sql NVARCHAR(MAX) =
        N'ALTER TABLE dbo.Teachers DROP CONSTRAINT ' + QUOTENAME(@ConstraintName) + N';';

    EXEC sp_executesql @sql;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Teachers')
      AND name = N'UQ_Teachers_TeacherCode'
)
BEGIN
    DROP INDEX UQ_Teachers_TeacherCode ON dbo.Teachers;
END;
GO

CREATE UNIQUE INDEX UQ_Teachers_TeacherCode
ON dbo.Teachers(TeacherCode)
WHERE IsDeleted = 0;
GO



USE EduBridgeDB;
GO

/* Students: bỏ UNIQUE constraint trên StudentCode nếu có */
DECLARE @sql NVARCHAR(MAX);

SELECT @sql = N'ALTER TABLE dbo.Students DROP CONSTRAINT ' + QUOTENAME(kc.name)
FROM sys.key_constraints kc
JOIN sys.index_columns ic
    ON ic.object_id = kc.parent_object_id
   AND ic.index_id = kc.unique_index_id
JOIN sys.columns c
    ON c.object_id = ic.object_id
   AND c.column_id = ic.column_id
WHERE kc.parent_object_id = OBJECT_ID(N'dbo.Students')
  AND kc.type = N'UQ'
  AND c.name = N'StudentCode';

IF @sql IS NOT NULL
    EXEC sp_executesql @sql;
GO

/* Students: bỏ UNIQUE index cũ nếu là index thường */
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Students')
      AND name = N'UQ__Students__1FC8860421D6BBDD'
)
BEGIN
    DROP INDEX UQ__Students__1FC8860421D6BBDD ON dbo.Students;
END;
GO

/* Students: unique mới theo từng trung tâm */
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Students')
      AND name = N'UX_Students_CenterId_StudentCode'
)
BEGIN
    CREATE UNIQUE INDEX UX_Students_CenterId_StudentCode
    ON dbo.Students(CenterId, StudentCode);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Students')
      AND name = N'IX_Students_CenterId_ParentUserId'
)
BEGIN
    CREATE INDEX IX_Students_CenterId_ParentUserId
    ON dbo.Students(CenterId, ParentUserId);
END;
GO

/* Teachers: bỏ UNIQUE constraint trên TeacherCode nếu có */
DECLARE @sql NVARCHAR(MAX);

SELECT @sql = N'ALTER TABLE dbo.Teachers DROP CONSTRAINT ' + QUOTENAME(kc.name)
FROM sys.key_constraints kc
JOIN sys.index_columns ic
    ON ic.object_id = kc.parent_object_id
   AND ic.index_id = kc.unique_index_id
JOIN sys.columns c
    ON c.object_id = ic.object_id
   AND c.column_id = ic.column_id
WHERE kc.parent_object_id = OBJECT_ID(N'dbo.Teachers')
  AND kc.type = N'UQ'
  AND c.name = N'TeacherCode';

IF @sql IS NOT NULL
    EXEC sp_executesql @sql;
GO

/* Teachers: bỏ UNIQUE index cũ nếu là index thường */
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Teachers')
      AND name = N'UQ_Teachers_TeacherCode'
)
BEGIN
    DROP INDEX UQ_Teachers_TeacherCode ON dbo.Teachers;
END;
GO

/* Teachers: unique mới theo từng trung tâm, bỏ qua soft-deleted */
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Teachers')
      AND name = N'UQ_Teachers_CenterId_TeacherCode'
)
BEGIN
    CREATE UNIQUE INDEX UQ_Teachers_CenterId_TeacherCode
    ON dbo.Teachers(CenterId, TeacherCode)
    WHERE IsDeleted = 0;
END;
GO


USE EduBridgeDB;
GO

IF COL_LENGTH(N'dbo.Students', N'IsDeleted') IS NULL
BEGIN
    ALTER TABLE dbo.Students
    ADD IsDeleted BIT NOT NULL
        CONSTRAINT DF_Students_IsDeleted DEFAULT 0;
END;
GO

IF COL_LENGTH(N'dbo.Students', N'DeletedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Students
    ADD DeletedAt DATETIME2 NULL;
END;
GO

IF COL_LENGTH(N'dbo.Students', N'DeletedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Students
    ADD DeletedByUserId INT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Students')
      AND name = N'IX_Students_CenterId_IsDeleted_Status_StudentId'
)
BEGIN
    CREATE INDEX IX_Students_CenterId_IsDeleted_Status_StudentId
    ON dbo.Students(CenterId, IsDeleted, Status, StudentId DESC);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Students')
      AND name = N'IX_Students_ParentUserId_IsDeleted'
)
BEGIN
    CREATE INDEX IX_Students_ParentUserId_IsDeleted
    ON dbo.Students(ParentUserId, IsDeleted);
END;
GO


/* Bỏ unique cũ nếu đang unique toàn bộ StudentCode */
DECLARE @sql NVARCHAR(MAX);

SELECT @sql = N'ALTER TABLE dbo.Students DROP CONSTRAINT ' + QUOTENAME(kc.name)
FROM sys.key_constraints kc
JOIN sys.index_columns ic
    ON ic.object_id = kc.parent_object_id
   AND ic.index_id = kc.unique_index_id
JOIN sys.columns c
    ON c.object_id = ic.object_id
   AND c.column_id = ic.column_id
WHERE kc.parent_object_id = OBJECT_ID(N'dbo.Students')
  AND kc.type = N'UQ'
  AND c.name = N'StudentCode';

IF @sql IS NOT NULL
    EXEC sp_executesql @sql;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Students')
      AND name = N'UQ__Students__1FC8860421D6BBDD'
)
BEGIN
    DROP INDEX UQ__Students__1FC8860421D6BBDD ON dbo.Students;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Students')
      AND name = N'UX_Students_CenterId_StudentCode'
)
BEGIN
    DROP INDEX UX_Students_CenterId_StudentCode ON dbo.Students;
END;
GO

CREATE UNIQUE INDEX UX_Students_CenterId_StudentCode_NotDeleted
ON dbo.Students(CenterId, StudentCode)
WHERE IsDeleted = 0;
GO 




USE EduBridgeDB;
GO

/* 1. Students soft delete + extended fields */
IF COL_LENGTH(N'dbo.Students', N'IsDeleted') IS NULL
BEGIN
    ALTER TABLE dbo.Students
    ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Students_IsDeleted DEFAULT 0;
END;
GO

IF COL_LENGTH(N'dbo.Students', N'DeletedAt') IS NULL
    ALTER TABLE dbo.Students ADD DeletedAt DATETIME2 NULL;
GO

IF COL_LENGTH(N'dbo.Students', N'DeletedByUserId') IS NULL
    ALTER TABLE dbo.Students ADD DeletedByUserId INT NULL;
GO

IF COL_LENGTH(N'dbo.Students', N'Ethnicity') IS NULL
    ALTER TABLE dbo.Students ADD Ethnicity NVARCHAR(50) NULL;
GO

IF COL_LENGTH(N'dbo.Students', N'Religion') IS NULL
    ALTER TABLE dbo.Students ADD Religion NVARCHAR(50) NULL;
GO

IF COL_LENGTH(N'dbo.Students', N'IdentityNumber') IS NULL
    ALTER TABLE dbo.Students ADD IdentityNumber NVARCHAR(20) NULL;
GO

IF COL_LENGTH(N'dbo.Students', N'IdentityIssuedDate') IS NULL
    ALTER TABLE dbo.Students ADD IdentityIssuedDate DATE NULL;
GO

IF COL_LENGTH(N'dbo.Students', N'IdentityIssuedPlace') IS NULL
    ALTER TABLE dbo.Students ADD IdentityIssuedPlace NVARCHAR(150) NULL;
GO

IF COL_LENGTH(N'dbo.Students', N'PermanentAddress') IS NULL
    ALTER TABLE dbo.Students ADD PermanentAddress NVARCHAR(255) NULL;
GO

IF COL_LENGTH(N'dbo.Students', N'Hometown') IS NULL
    ALTER TABLE dbo.Students ADD Hometown NVARCHAR(150) NULL;
GO

IF COL_LENGTH(N'dbo.Students', N'PlaceOfBirth') IS NULL
    ALTER TABLE dbo.Students ADD PlaceOfBirth NVARCHAR(150) NULL;
GO

/* 2. CenterUsers: user thuộc trung tâm */
IF OBJECT_ID(N'dbo.CenterUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CenterUsers
    (
        CenterId INT NOT NULL,
        UserId INT NOT NULL,
        UserType NVARCHAR(20) NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_CenterUsers_CreatedAt DEFAULT SYSDATETIME(),

        CONSTRAINT PK_CenterUsers PRIMARY KEY (CenterId, UserId),
        CONSTRAINT FK_CenterUsers_Centers FOREIGN KEY (CenterId) REFERENCES dbo.Centers(CenterId),
        CONSTRAINT FK_CenterUsers_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_CenterUsers_UserType CHECK (UserType IN (N'OWNER', N'TEACHER', N'PARENT'))
    );
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.CenterUsers')
      AND name = N'IX_CenterUsers_UserId_UserType'
)
BEGIN
    CREATE INDEX IX_CenterUsers_UserId_UserType
    ON dbo.CenterUsers(UserId, UserType);
END;
GO

/* 3. Backfill CenterUsers */
INSERT INTO dbo.CenterUsers (CenterId, UserId, UserType)
SELECT c.CenterId, c.OwnerUserId, N'OWNER'
FROM dbo.Centers c
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.CenterUsers cu
    WHERE cu.CenterId = c.CenterId
      AND cu.UserId = c.OwnerUserId
);
GO

INSERT INTO dbo.CenterUsers (CenterId, UserId, UserType)
SELECT DISTINCT t.CenterId, t.UserId, N'TEACHER'
FROM dbo.Teachers t
WHERE ISNULL(t.IsDeleted, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.CenterUsers cu
      WHERE cu.CenterId = t.CenterId
        AND cu.UserId = t.UserId
  );
GO

INSERT INTO dbo.CenterUsers (CenterId, UserId, UserType)
SELECT DISTINCT s.CenterId, s.ParentUserId, N'PARENT'
FROM dbo.Students s
WHERE ISNULL(s.IsDeleted, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.CenterUsers cu
      WHERE cu.CenterId = s.CenterId
        AND cu.UserId = s.ParentUserId
  );
GO

/* 4. Drop unique StudentCode toàn hệ thống nếu còn */
DECLARE @sql NVARCHAR(MAX);

SELECT @sql = N'ALTER TABLE dbo.Students DROP CONSTRAINT ' + QUOTENAME(kc.name)
FROM sys.key_constraints kc
JOIN sys.index_columns ic
    ON ic.object_id = kc.parent_object_id
   AND ic.index_id = kc.unique_index_id
JOIN sys.columns c
    ON c.object_id = ic.object_id
   AND c.column_id = ic.column_id
WHERE kc.parent_object_id = OBJECT_ID(N'dbo.Students')
  AND kc.type = N'UQ'
  AND c.name = N'StudentCode';

IF @sql IS NOT NULL EXEC sp_executesql @sql;
GO

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Students')
      AND name = N'UQ__Students__1FC8860421D6BBDD'
)
BEGIN
    DROP INDEX UQ__Students__1FC8860421D6BBDD ON dbo.Students;
END;
GO

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Students')
      AND name = N'UX_Students_CenterId_StudentCode'
)
BEGIN
    DROP INDEX UX_Students_CenterId_StudentCode ON dbo.Students;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Students')
      AND name = N'UX_Students_CenterId_StudentCode_NotDeleted'
)
BEGIN
    CREATE UNIQUE INDEX UX_Students_CenterId_StudentCode_NotDeleted
    ON dbo.Students(CenterId, StudentCode)
    WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Students')
      AND name = N'IX_Students_CenterId_IsDeleted_Status_StudentId'
)
BEGIN
    CREATE INDEX IX_Students_CenterId_IsDeleted_Status_StudentId
    ON dbo.Students(CenterId, IsDeleted, Status, StudentId DESC);
END;
GO


USE EduBridgeDB;
GO

/* =========================================================
   1. Tạo bảng CenterUsers nếu chưa có
   ========================================================= */

IF OBJECT_ID(N'dbo.CenterUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CenterUsers
    (
        CenterUserId INT IDENTITY(1,1) NOT NULL,
        CenterId INT NOT NULL,
        UserId INT NOT NULL,
        UserType NVARCHAR(20) NOT NULL,
        Status NVARCHAR(20) NOT NULL
            CONSTRAINT DF_CenterUsers_Status DEFAULT N'Active',
        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_CenterUsers_CreatedAt DEFAULT SYSDATETIME(),

        CONSTRAINT PK_CenterUsers
            PRIMARY KEY (CenterUserId),

        CONSTRAINT FK_CenterUsers_Centers
            FOREIGN KEY (CenterId) REFERENCES dbo.Centers(CenterId),

        CONSTRAINT FK_CenterUsers_Users
            FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),

        CONSTRAINT CK_CenterUsers_UserType
            CHECK (UserType IN (N'OWNER', N'TEACHER', N'PARENT')),

        CONSTRAINT CK_CenterUsers_Status
            CHECK (Status IN (N'Active', N'Inactive'))
    );
END;
GO


/* =========================================================
   2. Nếu bảng CenterUsers đã tồn tại dạng cũ:
      - thêm CenterUserId nếu thiếu
      - thêm Status nếu thiếu
      - đảm bảo CreatedAt nếu thiếu
   ========================================================= */

IF COL_LENGTH(N'dbo.CenterUsers', N'CenterUserId') IS NULL
BEGIN
    ALTER TABLE dbo.CenterUsers
    ADD CenterUserId INT IDENTITY(1,1) NOT NULL;
END;
GO

IF COL_LENGTH(N'dbo.CenterUsers', N'Status') IS NULL
BEGIN
    ALTER TABLE dbo.CenterUsers
    ADD Status NVARCHAR(20) NOT NULL
        CONSTRAINT DF_CenterUsers_Status DEFAULT N'Active';
END;
GO

IF COL_LENGTH(N'dbo.CenterUsers', N'CreatedAt') IS NULL
BEGIN
    ALTER TABLE dbo.CenterUsers
    ADD CreatedAt DATETIME2 NOT NULL
        CONSTRAINT DF_CenterUsers_CreatedAt DEFAULT SYSDATETIME();
END;
GO


/* =========================================================
   3. Drop primary key cũ nếu đang là (CenterId, UserId)
      để đổi sang PK CenterUserId
   ========================================================= */

DECLARE @pkName SYSNAME;

SELECT @pkName = kc.name
FROM sys.key_constraints kc
WHERE kc.parent_object_id = OBJECT_ID(N'dbo.CenterUsers')
  AND kc.type = N'PK';

IF @pkName IS NOT NULL
BEGIN
    DECLARE @sql NVARCHAR(MAX);

    SELECT @sql = N'ALTER TABLE dbo.CenterUsers DROP CONSTRAINT ' + QUOTENAME(@pkName);

    EXEC sp_executesql @sql;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.CenterUsers')
      AND type = N'PK'
      AND name = N'PK_CenterUsers'
)
BEGIN
    ALTER TABLE dbo.CenterUsers
    ADD CONSTRAINT PK_CenterUsers
    PRIMARY KEY (CenterUserId);
END;
GO


/* =========================================================
   4. Đảm bảo FK tồn tại
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.CenterUsers')
      AND name = N'FK_CenterUsers_Centers'
)
BEGIN
    ALTER TABLE dbo.CenterUsers
    ADD CONSTRAINT FK_CenterUsers_Centers
    FOREIGN KEY (CenterId) REFERENCES dbo.Centers(CenterId);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.CenterUsers')
      AND name = N'FK_CenterUsers_Users'
)
BEGIN
    ALTER TABLE dbo.CenterUsers
    ADD CONSTRAINT FK_CenterUsers_Users
    FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId);
END;
GO


/* =========================================================
   5. Đảm bảo CHECK constraints
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.CenterUsers')
      AND name = N'CK_CenterUsers_UserType'
)
BEGIN
    ALTER TABLE dbo.CenterUsers
    ADD CONSTRAINT CK_CenterUsers_UserType
    CHECK (UserType IN (N'OWNER', N'TEACHER', N'PARENT'));
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.CenterUsers')
      AND name = N'CK_CenterUsers_Status'
)
BEGIN
    ALTER TABLE dbo.CenterUsers
    ADD CONSTRAINT CK_CenterUsers_Status
    CHECK (Status IN (N'Active', N'Inactive'));
END;
GO


/* =========================================================
   6. Xóa dữ liệu trùng trước khi tạo unique index
   Giữ lại dòng có CenterUserId nhỏ nhất
   ========================================================= */

;WITH Duplicates AS
(
    SELECT
        CenterUserId,
        ROW_NUMBER() OVER (
            PARTITION BY CenterId, UserId, UserType
            ORDER BY CenterUserId
        ) AS RowNum
    FROM dbo.CenterUsers
)
DELETE FROM Duplicates
WHERE RowNum > 1;
GO


/* =========================================================
   7. Unique theo CenterId + UserId + UserType
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.CenterUsers')
      AND name = N'UX_CenterUsers_Center_User_Type'
)
BEGIN
    CREATE UNIQUE INDEX UX_CenterUsers_Center_User_Type
    ON dbo.CenterUsers(CenterId, UserId, UserType);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.CenterUsers')
      AND name = N'IX_CenterUsers_UserId_UserType_Status'
)
BEGIN
    CREATE INDEX IX_CenterUsers_UserId_UserType_Status
    ON dbo.CenterUsers(UserId, UserType, Status);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.CenterUsers')
      AND name = N'IX_CenterUsers_Center_UserType_Status'
)
BEGIN
    CREATE INDEX IX_CenterUsers_Center_UserType_Status
    ON dbo.CenterUsers(CenterId, UserType, Status);
END;
GO


/* =========================================================
   8. Backfill OWNER
   ========================================================= */

INSERT INTO dbo.CenterUsers
    (CenterId, UserId, UserType, Status)
SELECT
    c.CenterId,
    c.OwnerUserId,
    N'OWNER',
    CASE
        WHEN c.Status = N'Active' THEN N'Active'
        ELSE N'Inactive'
    END
FROM dbo.Centers c
WHERE c.OwnerUserId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.CenterUsers cu
      WHERE cu.CenterId = c.CenterId
        AND cu.UserId = c.OwnerUserId
        AND cu.UserType = N'OWNER'
  );
GO


/* =========================================================
   9. Backfill TEACHER
   ========================================================= */

INSERT INTO dbo.CenterUsers
    (CenterId, UserId, UserType, Status)
SELECT DISTINCT
    t.CenterId,
    t.UserId,
    N'TEACHER',
    CASE
        WHEN ISNULL(t.IsDeleted, 0) = 0
         AND t.Status = N'Active'
         AND u.Status = N'Active'
         AND ISNULL(u.IsDeleted, 0) = 0
        THEN N'Active'
        ELSE N'Inactive'
    END
FROM dbo.Teachers t
JOIN dbo.Users u
    ON u.UserId = t.UserId
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.CenterUsers cu
    WHERE cu.CenterId = t.CenterId
      AND cu.UserId = t.UserId
      AND cu.UserType = N'TEACHER'
);
GO


/* =========================================================
   10. Backfill PARENT
   ========================================================= */

INSERT INTO dbo.CenterUsers
    (CenterId, UserId, UserType, Status)
SELECT DISTINCT
    s.CenterId,
    s.ParentUserId,
    N'PARENT',
    CASE
        WHEN ISNULL(s.IsDeleted, 0) = 0
         AND u.Status = N'Active'
         AND ISNULL(u.IsDeleted, 0) = 0
        THEN N'Active'
        ELSE N'Inactive'
    END
FROM dbo.Students s
JOIN dbo.Users u
    ON u.UserId = s.ParentUserId
WHERE s.ParentUserId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.CenterUsers cu
      WHERE cu.CenterId = s.CenterId
        AND cu.UserId = s.ParentUserId
        AND cu.UserType = N'PARENT'
  );
GO


/* =========================================================
   11. Đồng bộ lại status nếu đã có dòng CenterUsers trước đó
   ========================================================= */

UPDATE cu
SET cu.Status =
    CASE
        WHEN c.Status = N'Active'
         AND ISNULL(u.IsDeleted, 0) = 0
         AND u.Status = N'Active'
        THEN N'Active'
        ELSE N'Inactive'
    END
FROM dbo.CenterUsers cu
JOIN dbo.Centers c
    ON c.CenterId = cu.CenterId
JOIN dbo.Users u
    ON u.UserId = cu.UserId
WHERE cu.UserType = N'OWNER';
GO

UPDATE cu
SET cu.Status =
    CASE
        WHEN ISNULL(t.IsDeleted, 0) = 0
         AND t.Status = N'Active'
         AND ISNULL(u.IsDeleted, 0) = 0
         AND u.Status = N'Active'
        THEN N'Active'
        ELSE N'Inactive'
    END
FROM dbo.CenterUsers cu
JOIN dbo.Teachers t
    ON t.CenterId = cu.CenterId
   AND t.UserId = cu.UserId
JOIN dbo.Users u
    ON u.UserId = cu.UserId
WHERE cu.UserType = N'TEACHER';
GO

UPDATE cu
SET cu.Status =
    CASE
        WHEN EXISTS (
            SELECT 1
            FROM dbo.Students s
            WHERE s.CenterId = cu.CenterId
              AND s.ParentUserId = cu.UserId
              AND ISNULL(s.IsDeleted, 0) = 0
        )
        AND ISNULL(u.IsDeleted, 0) = 0
        AND u.Status = N'Active'
        THEN N'Active'
        ELSE N'Inactive'
    END
FROM dbo.CenterUsers cu
JOIN dbo.Users u
    ON u.UserId = cu.UserId
WHERE cu.UserType = N'PARENT';
GO


/* =========================================================
   12. Kiểm tra kết quả
   ========================================================= */

SELECT
    cu.CenterUserId,
    cu.CenterId,
    c.CenterName,
    cu.UserId,
    u.FullName,
    cu.UserType,
    cu.Status,
    cu.CreatedAt
FROM dbo.CenterUsers cu
JOIN dbo.Centers c
    ON c.CenterId = cu.CenterId
JOIN dbo.Users u
    ON u.UserId = cu.UserId
ORDER BY
    cu.CenterId,
    cu.UserType,
    u.FullName;
GO


USE EduBridgeDB;
GO

/* =========================================================
   1. Rooms: thêm soft delete columns
   ========================================================= */

IF COL_LENGTH(N'dbo.Rooms', N'IsDeleted') IS NULL
BEGIN
    ALTER TABLE dbo.Rooms
    ADD IsDeleted BIT NOT NULL
        CONSTRAINT DF_Rooms_IsDeleted DEFAULT (0);
END;
GO

IF COL_LENGTH(N'dbo.Rooms', N'DeletedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Rooms
    ADD DeletedAt DATETIME2 NULL;
END;
GO

IF COL_LENGTH(N'dbo.Rooms', N'DeletedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Rooms
    ADD DeletedByUserId INT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Rooms_DeletedByUser'
      AND parent_object_id = OBJECT_ID(N'dbo.Rooms')
)
BEGIN
    ALTER TABLE dbo.Rooms
    ADD CONSTRAINT FK_Rooms_DeletedByUser
        FOREIGN KEY (DeletedByUserId)
        REFERENCES dbo.Users(UserId);
END;
GO

/* =========================================================
   2. Rooms: chuẩn hóa status
   ========================================================= */

IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Rooms_Status'
      AND parent_object_id = OBJECT_ID(N'dbo.Rooms')
)
BEGIN
    ALTER TABLE dbo.Rooms
    DROP CONSTRAINT CK_Rooms_Status;
END;
GO

ALTER TABLE dbo.Rooms
ADD CONSTRAINT CK_Rooms_Status
CHECK (Status IN (N'Active', N'Inactive', N'Maintenance'));
GO

/* =========================================================
   3. Index phục vụ list/search/filter phòng
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Rooms')
      AND name = N'IX_Rooms_CenterId_IsDeleted_Status_RoomId'
)
BEGIN
    CREATE INDEX IX_Rooms_CenterId_IsDeleted_Status_RoomId
    ON dbo.Rooms(CenterId, IsDeleted, Status, RoomId DESC);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Rooms')
      AND name = N'IX_Rooms_CenterId_IsDeleted_RoomCode'
)
BEGIN
    CREATE INDEX IX_Rooms_CenterId_IsDeleted_RoomCode
    ON dbo.Rooms(CenterId, IsDeleted, RoomCode);
END;
GO

/* =========================================================
   4. Unique mã phòng chỉ áp dụng với phòng chưa bị xóa mềm
   ========================================================= */

DECLARE @constraintName SYSNAME;
DECLARE @sql NVARCHAR(MAX);

SELECT TOP 1 @constraintName = kc.name
FROM sys.key_constraints kc
WHERE kc.parent_object_id = OBJECT_ID(N'dbo.Rooms')
  AND kc.type = N'UQ'
  AND kc.name = N'UQ_Rooms_Center_RoomCode';

IF @constraintName IS NOT NULL
BEGIN
    SET @sql = N'ALTER TABLE dbo.Rooms DROP CONSTRAINT ' + QUOTENAME(@constraintName);
    EXEC sp_executesql @sql;
END;
GO

DECLARE @indexName SYSNAME;
DECLARE @sql NVARCHAR(MAX);

SELECT TOP 1 @indexName = i.name
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID(N'dbo.Rooms')
  AND i.name = N'UQ_Rooms_Center_RoomCode'
  AND NOT EXISTS (
      SELECT 1
      FROM sys.key_constraints kc
      WHERE kc.parent_object_id = i.object_id
        AND kc.unique_index_id = i.index_id
  );

IF @indexName IS NOT NULL
BEGIN
    SET @sql = N'DROP INDEX ' + QUOTENAME(@indexName) + N' ON dbo.Rooms';
    EXEC sp_executesql @sql;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Rooms')
      AND name = N'UX_Rooms_CenterId_RoomCode_NotDeleted'
)
BEGIN
    CREATE UNIQUE INDEX UX_Rooms_CenterId_RoomCode_NotDeleted
    ON dbo.Rooms(CenterId, RoomCode)
    WHERE IsDeleted = 0;
END;
GO

/* =========================================================
   5. Recreate vw_RoomOverview để bỏ phòng đã xóa mềm
   ========================================================= */

IF OBJECT_ID(N'dbo.vw_RoomOverview', N'V') IS NOT NULL
BEGIN
    DROP VIEW dbo.vw_RoomOverview;
END;
GO

CREATE VIEW dbo.vw_RoomOverview AS
SELECT
    r.RoomId,
    r.CenterId,
    r.RoomCode,
    r.RoomName,
    r.Capacity,
    r.Location,
    r.Note,
    r.Status,
    COUNT(c.ClassId) AS TotalClasses,
    SUM(CASE WHEN c.Status = N'Active' THEN 1 ELSE 0 END) AS ActiveClasses,
    MAX(c.ClassName) AS LatestClassName,
    MAX(c.ScheduleText) AS LatestScheduleText
FROM dbo.Rooms r
LEFT JOIN dbo.Classes c
    ON c.RoomId = r.RoomId
WHERE r.IsDeleted = 0
GROUP BY
    r.RoomId,
    r.CenterId,
    r.RoomCode,
    r.RoomName,
    r.Capacity,
    r.Location,
    r.Note,
    r.Status;
GO

/* =========================================================
   6. Kiểm tra nhanh
   ========================================================= */

SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = N'Rooms'
ORDER BY ORDINAL_POSITION;
GO

SELECT
    RoomId,
    RoomCode,
    RoomName,
    Status,
    IsDeleted,
    DeletedAt,
    DeletedByUserId
FROM dbo.Rooms
ORDER BY RoomId DESC;
GO


USE EduBridgeDB;
GO

/* Chuẩn hóa Courses chỉ còn 2 trạng thái:
   Active   = Đang sử dụng
   Inactive = Tạm dừng
*/

IF OBJECT_ID(N'dbo.Courses', N'U') IS NULL
BEGIN
    THROW 50000, 'Table dbo.Courses does not exist.', 1;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Courses_Status'
      AND parent_object_id = OBJECT_ID(N'dbo.Courses')
)
BEGIN
    ALTER TABLE dbo.Courses DROP CONSTRAINT CK_Courses_Status;
END;
GO

UPDATE dbo.Courses
SET Status = N'Inactive'
WHERE Status IS NULL
   OR LTRIM(RTRIM(Status)) = N''
   OR Status NOT IN (N'Active', N'Inactive');
GO

ALTER TABLE dbo.Courses
ALTER COLUMN Status NVARCHAR(20) NOT NULL;
GO

ALTER TABLE dbo.Courses
ADD CONSTRAINT CK_Courses_Status
CHECK (Status IN (N'Active', N'Inactive'));
GO

SELECT
    Status,
    COUNT(*) AS TotalRows
FROM dbo.Courses
GROUP BY Status;
GO


USE EduBridgeDB;
GO

IF COL_LENGTH(N'dbo.Courses', N'CourseCode') IS NULL
BEGIN
    ALTER TABLE dbo.Courses
    ADD CourseCode NVARCHAR(30) NULL;
END;
GO

;WITH NumberedCourses AS
(
    SELECT
        CourseId,
        ROW_NUMBER() OVER (ORDER BY CourseId) AS RowNum
    FROM dbo.Courses
    WHERE CourseCode IS NULL
       OR LTRIM(RTRIM(CourseCode)) = N''
)
UPDATE c
SET CourseCode = CONCAT(N'MH', RIGHT(CONCAT(N'0000', n.RowNum), 4))
FROM dbo.Courses c
JOIN NumberedCourses n
    ON n.CourseId = c.CourseId;
GO

ALTER TABLE dbo.Courses
ALTER COLUMN CourseCode NVARCHAR(30) NOT NULL;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Courses')
      AND name = N'IX_Courses_CenterId_CourseCode'
)
BEGIN
    CREATE UNIQUE INDEX IX_Courses_CenterId_CourseCode
    ON dbo.Courses(CenterId, CourseCode);
END;
GO

SELECT
    CourseId,
    CenterId,
    CourseCode,
    CourseName,
    Status
FROM dbo.Courses
ORDER BY CenterId, CourseId;
GO