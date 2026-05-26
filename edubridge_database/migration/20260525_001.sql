USE EduBridgeDB;
GO

IF COL_LENGTH(N'dbo.Users', N'DateOfBirth') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD DateOfBirth DATE NULL;
END;
GO

IF COL_LENGTH(N'dbo.Users', N'Gender') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD Gender NVARCHAR(10) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Users', N'Address') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD Address NVARCHAR(255) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Teachers', N'TeacherCode') IS NULL
BEGIN
    ALTER TABLE dbo.Teachers
    ADD TeacherCode NVARCHAR(30) NULL;
END;
GO

;WITH NumberedTeachers AS
(
    SELECT
        TeacherId,
        ROW_NUMBER() OVER (PARTITION BY CenterId ORDER BY TeacherId) AS RowNum
    FROM dbo.Teachers
    WHERE TeacherCode IS NULL OR LTRIM(RTRIM(TeacherCode)) = N''
)
UPDATE t
SET TeacherCode = CONCAT(N'GV-', t.CenterId, N'-', RIGHT(CONCAT(N'0000', n.RowNum), 4))
FROM dbo.Teachers t
JOIN NumberedTeachers n ON n.TeacherId = t.TeacherId;
GO

ALTER TABLE dbo.Teachers
ALTER COLUMN TeacherCode NVARCHAR(30) NOT NULL;
GO

IF OBJECT_ID(N'dbo.TeacherCodeCounters', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeacherCodeCounters (
        CenterId INT NOT NULL PRIMARY KEY,
        LastNumber INT NOT NULL DEFAULT 0,

        CONSTRAINT FK_TeacherCodeCounters_Centers
            FOREIGN KEY (CenterId) REFERENCES dbo.Centers(CenterId),

        CONSTRAINT CK_TeacherCodeCounters_LastNumber
            CHECK (LastNumber >= 0)
    );
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.Teachers')
      AND name = N'UQ_Teachers_TeacherCode'
)
BEGIN
    ALTER TABLE dbo.Teachers
    ADD CONSTRAINT UQ_Teachers_TeacherCode UNIQUE (TeacherCode);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Teachers')
      AND name = N'IX_Teachers_CenterId_TeacherCode'
)
BEGIN
    CREATE INDEX IX_Teachers_CenterId_TeacherCode
    ON dbo.Teachers(CenterId, TeacherCode);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_Users_Gender'
)
BEGIN
    ALTER TABLE dbo.Users
    ADD CONSTRAINT CK_Users_Gender
    CHECK (Gender IS NULL OR Gender IN (N'Nam', N'Nữ', N'Khác'));
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Users')
      AND name = N'UX_Users_NormalizedPhoneNumber_NotNull'
)
BEGIN
    CREATE UNIQUE INDEX UX_Users_NormalizedPhoneNumber_NotNull
    ON dbo.Users(NormalizedPhoneNumber)
    WHERE NormalizedPhoneNumber IS NOT NULL;
END;
GO


/*===26/05/2026===*/
USE EduBridgeDB;
GO

/* =========================================================
   1. Users.Email cho phép NULL
   Vì giáo viên có thể login bằng SĐT mà không cần email
   ========================================================= */

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Users')
      AND name = N'UQ__Users__A9D105346CC3F558'
)
BEGIN
    ALTER TABLE dbo.Users
    DROP CONSTRAINT UQ__Users__A9D105346CC3F558;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Users')
      AND name = N'IX_Users_Email'
)
BEGIN
    DROP INDEX IX_Users_Email ON dbo.Users;
END;
GO

ALTER TABLE dbo.Users
ALTER COLUMN Email NVARCHAR(150) NULL;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Users')
      AND name = N'UX_Users_Email_NotNull'
)
BEGIN
    CREATE UNIQUE INDEX UX_Users_Email_NotNull
    ON dbo.Users(Email)
    WHERE Email IS NOT NULL;
END;
GO


/* =========================================================
   2. Users phải có ít nhất Email hoặc PhoneNumber
   ========================================================= */

IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Users_EmailOrPhone'
      AND parent_object_id = OBJECT_ID(N'dbo.Users')
)
BEGIN
    ALTER TABLE dbo.Users
    DROP CONSTRAINT CK_Users_EmailOrPhone;
END;
GO

ALTER TABLE dbo.Users
ADD CONSTRAINT CK_Users_EmailOrPhone
CHECK (
    Email IS NOT NULL
    OR PhoneNumber IS NOT NULL
);
GO


/* =========================================================
   3. Gender chỉ cho Nam / Nữ
   ========================================================= */

IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Users_Gender'
      AND parent_object_id = OBJECT_ID(N'dbo.Users')
)
BEGIN
    ALTER TABLE dbo.Users
    DROP CONSTRAINT CK_Users_Gender;
END;
GO

UPDATE dbo.Users
SET Gender = NULL
WHERE Gender IS NOT NULL
  AND Gender NOT IN (N'Nam', N'Nữ');
GO

ALTER TABLE dbo.Users
ADD CONSTRAINT CK_Users_Gender
CHECK (
    Gender IS NULL
    OR Gender IN (N'Nam', N'Nữ')
);
GO


/* =========================================================
   4. Status constraint cho Users
   ========================================================= */

IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Users_Status'
      AND parent_object_id = OBJECT_ID(N'dbo.Users')
)
BEGIN
    ALTER TABLE dbo.Users
    DROP CONSTRAINT CK_Users_Status;
END;
GO

UPDATE dbo.Users
SET Status = N'Active'
WHERE Status IS NULL
   OR Status NOT IN (N'Active', N'Inactive', N'Locked');
GO

ALTER TABLE dbo.Users
ADD CONSTRAINT CK_Users_Status
CHECK (
    Status IN (N'Active', N'Inactive', N'Locked')
);
GO


/* =========================================================
   5. Status constraint cho Teachers
   ========================================================= */

IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Teachers_Status'
      AND parent_object_id = OBJECT_ID(N'dbo.Teachers')
)
BEGIN
    ALTER TABLE dbo.Teachers
    DROP CONSTRAINT CK_Teachers_Status;
END;
GO

UPDATE dbo.Teachers
SET Status = N'Active'
WHERE Status IS NULL
   OR Status NOT IN (N'Active', N'Inactive');
GO

ALTER TABLE dbo.Teachers
ADD CONSTRAINT CK_Teachers_Status
CHECK (
    Status IN (N'Active', N'Inactive')
);
GO


/* =========================================================
   6. Phone unique filtered index
   Đảm bảo login bằng SĐT không bị nhập nhằng
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Users')
      AND name = N'UX_Users_NormalizedPhoneNumber_NotNull'
)
BEGIN
    CREATE UNIQUE INDEX UX_Users_NormalizedPhoneNumber_NotNull
    ON dbo.Users(NormalizedPhoneNumber)
    WHERE NormalizedPhoneNumber IS NOT NULL;
END;
GO


/* =========================================================
   7. Kiểm tra sau khi chạy
   ========================================================= */

SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = N'Users'
  AND COLUMN_NAME IN (
      N'Email',
      N'PhoneNumber',
      N'NormalizedPhoneNumber',
      N'Gender',
      N'Status'
  )
ORDER BY ORDINAL_POSITION;
GO

SELECT
    cc.name AS ConstraintName,
    OBJECT_NAME(cc.parent_object_id) AS TableName,
    cc.definition
FROM sys.check_constraints cc
WHERE cc.parent_object_id IN (
    OBJECT_ID(N'dbo.Users'),
    OBJECT_ID(N'dbo.Teachers')
)
ORDER BY TableName, ConstraintName;
GO

SELECT
    i.name AS IndexName,
    OBJECT_NAME(i.object_id) AS TableName,
    i.is_unique,
    i.filter_definition
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID(N'dbo.Users')
  AND i.name IN (
      N'UX_Users_Email_NotNull',
      N'UX_Users_NormalizedPhoneNumber_NotNull'
  );
GO

USE EduBridgeDB;
GO

/* =========================================================
   1. Add personal profile columns to Users
   ========================================================= */

IF COL_LENGTH(N'dbo.Users', N'Ethnicity') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD Ethnicity NVARCHAR(50) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Users', N'Religion') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD Religion NVARCHAR(50) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Users', N'IdentityNumber') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD IdentityNumber NVARCHAR(20) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Users', N'IdentityIssuedDate') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD IdentityIssuedDate DATE NULL;
END;
GO

IF COL_LENGTH(N'dbo.Users', N'IdentityIssuedPlace') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD IdentityIssuedPlace NVARCHAR(150) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Users', N'CurrentAddress') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD CurrentAddress NVARCHAR(255) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Users', N'PermanentAddress') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD PermanentAddress NVARCHAR(255) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Users', N'Hometown') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD Hometown NVARCHAR(150) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Users', N'PlaceOfBirth') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD PlaceOfBirth NVARCHAR(150) NULL;
END;
GO


/* =========================================================
   2. Unique filtered index for CMND/CCCD
   Cho phép NULL để user cũ không lỗi, nhưng nếu có nhập thì không được trùng
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Users')
      AND name = N'UX_Users_IdentityNumber_NotNull'
)
BEGIN
    CREATE UNIQUE INDEX UX_Users_IdentityNumber_NotNull
    ON dbo.Users(IdentityNumber)
    WHERE IdentityNumber IS NOT NULL;
END;
GO


/* =========================================================
   3. Check constraint for CMND/CCCD format
   Cho phép NULL, còn nếu nhập thì chỉ 9 hoặc 12 số
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Users_IdentityNumber_Format'
      AND parent_object_id = OBJECT_ID(N'dbo.Users')
)
BEGIN
    ALTER TABLE dbo.Users
    ADD CONSTRAINT CK_Users_IdentityNumber_Format
    CHECK (
        IdentityNumber IS NULL
        OR (
            IdentityNumber NOT LIKE N'%[^0-9]%'
            AND LEN(IdentityNumber) IN (9, 12)
        )
    );
END;
GO


/* =========================================================
   4. Check constraint for identity issued date
   Ngày cấp không được vượt ngày hiện tại
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Users_IdentityIssuedDate_NotFuture'
      AND parent_object_id = OBJECT_ID(N'dbo.Users')
)
BEGIN
    ALTER TABLE dbo.Users
    ADD CONSTRAINT CK_Users_IdentityIssuedDate_NotFuture
    CHECK (
        IdentityIssuedDate IS NULL
        OR IdentityIssuedDate <= CONVERT(date, GETDATE())
    );
END;
GO


/* =========================================================
   5. Quick verify
   ========================================================= */

SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = N'Users'
  AND COLUMN_NAME IN (
      N'Ethnicity',
      N'Religion',
      N'IdentityNumber',
      N'IdentityIssuedDate',
      N'IdentityIssuedPlace',
      N'CurrentAddress',
      N'PermanentAddress',
      N'Hometown',
      N'PlaceOfBirth'
  )
ORDER BY ORDINAL_POSITION;
GO

SELECT
    name AS IndexName,
    type_desc,
    is_unique,
    filter_definition
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'dbo.Users')
  AND name = N'UX_Users_IdentityNumber_NotNull';
GO

SELECT
    name AS ConstraintName,
    definition
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID(N'dbo.Users')
  AND name IN (
      N'CK_Users_IdentityNumber_Format',
      N'CK_Users_IdentityIssuedDate_NotFuture'
  );
GO