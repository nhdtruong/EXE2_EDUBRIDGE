USE EduBridgeDB;
GO

/* 1. Thêm cột soft delete cho Courses */
IF COL_LENGTH(N'dbo.Courses', N'IsDeleted') IS NULL
BEGIN
    ALTER TABLE dbo.Courses
    ADD IsDeleted BIT NOT NULL
        CONSTRAINT DF_Courses_IsDeleted DEFAULT (0);
END;
GO

IF COL_LENGTH(N'dbo.Courses', N'DeletedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Courses
    ADD DeletedAt DATETIME2 NULL;
END;
GO

IF COL_LENGTH(N'dbo.Courses', N'DeletedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Courses
    ADD DeletedByUserId INT NULL;
END;
GO

/* 2. FK người xóa */
IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Courses_DeletedByUser'
      AND parent_object_id = OBJECT_ID(N'dbo.Courses')
)
BEGIN
    ALTER TABLE dbo.Courses
    ADD CONSTRAINT FK_Courses_DeletedByUser
        FOREIGN KEY (DeletedByUserId) REFERENCES dbo.Users(UserId);
END;
GO

/* 3. Xóa unique constraint/index cũ trên CenterId + CourseCode nếu có */
DECLARE @dropConstraintSql NVARCHAR(MAX) = N'';

SELECT @dropConstraintSql +=
    N'ALTER TABLE dbo.Courses DROP CONSTRAINT ' + QUOTENAME(kc.name) + N';' + CHAR(13)
FROM sys.key_constraints kc
JOIN sys.index_columns ic1
    ON ic1.object_id = kc.parent_object_id
   AND ic1.index_id = kc.unique_index_id
   AND ic1.key_ordinal = 1
JOIN sys.columns c1
    ON c1.object_id = ic1.object_id
   AND c1.column_id = ic1.column_id
JOIN sys.index_columns ic2
    ON ic2.object_id = kc.parent_object_id
   AND ic2.index_id = kc.unique_index_id
   AND ic2.key_ordinal = 2
JOIN sys.columns c2
    ON c2.object_id = ic2.object_id
   AND c2.column_id = ic2.column_id
WHERE kc.parent_object_id = OBJECT_ID(N'dbo.Courses')
  AND kc.type = N'UQ'
  AND c1.name = N'CenterId'
  AND c2.name = N'CourseCode';

IF @dropConstraintSql <> N''
    EXEC sp_executesql @dropConstraintSql;
GO

DECLARE @dropIndexSql NVARCHAR(MAX) = N'';

SELECT @dropIndexSql +=
    N'DROP INDEX ' + QUOTENAME(i.name) + N' ON dbo.Courses;' + CHAR(13)
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID(N'dbo.Courses')
  AND i.is_unique = 1
  AND i.name <> N'UX_Courses_CenterId_CourseCode_NotDeleted'
  AND NOT EXISTS (
      SELECT 1
      FROM sys.key_constraints kc
      WHERE kc.parent_object_id = i.object_id
        AND kc.unique_index_id = i.index_id
  )
  AND EXISTS (
      SELECT 1
      FROM sys.index_columns ic
      JOIN sys.columns c
        ON c.object_id = ic.object_id
       AND c.column_id = ic.column_id
      WHERE ic.object_id = i.object_id
        AND ic.index_id = i.index_id
        AND ic.key_ordinal = 1
        AND c.name = N'CenterId'
  )
  AND EXISTS (
      SELECT 1
      FROM sys.index_columns ic
      JOIN sys.columns c
        ON c.object_id = ic.object_id
       AND c.column_id = ic.column_id
      WHERE ic.object_id = i.object_id
        AND ic.index_id = i.index_id
        AND ic.key_ordinal = 2
        AND c.name = N'CourseCode'
  );

IF @dropIndexSql <> N''
    EXEC sp_executesql @dropIndexSql;
GO

/* 4. Unique mã môn chỉ áp dụng với môn chưa bị xóa */
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Courses')
      AND name = N'UX_Courses_CenterId_CourseCode_NotDeleted'
)
BEGIN
    CREATE UNIQUE INDEX UX_Courses_CenterId_CourseCode_NotDeleted
    ON dbo.Courses(CenterId, CourseCode)
    WHERE IsDeleted = 0;
END;
GO

/* 5. Index tối ưu danh sách/filter */
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Courses')
      AND name = N'IX_Courses_CenterId_IsDeleted_CourseCode'
)
BEGIN
    CREATE INDEX IX_Courses_CenterId_IsDeleted_CourseCode
    ON dbo.Courses(CenterId, IsDeleted, CourseCode);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Courses')
      AND name = N'IX_Courses_CenterId_IsDeleted_Status_CourseId'
)
BEGIN
    CREATE INDEX IX_Courses_CenterId_IsDeleted_Status_CourseId
    ON dbo.Courses(CenterId, IsDeleted, Status, CourseId DESC);
END;
GO

/* 6. Kiểm tra */
SELECT
    CourseId,
    CenterId,
    CourseCode,
    CourseName,
    Status,
    IsDeleted,
    DeletedAt,
    DeletedByUserId
FROM dbo.Courses
ORDER BY CourseId DESC;
GO

USE EduBridgeDB;
GO

/* =========================================================
   StudyShifts - Quản lý ca học theo trung tâm
   ========================================================= */

IF OBJECT_ID(N'dbo.StudyShifts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StudyShifts
    (
        StudyShiftId INT IDENTITY(1,1) NOT NULL,
        CenterId INT NOT NULL,

        ShiftCode NVARCHAR(30) NOT NULL,
        ShiftName NVARCHAR(100) NOT NULL,

        StartTime TIME(0) NOT NULL,
        EndTime TIME(0) NOT NULL,

        Status NVARCHAR(20) NOT NULL
            CONSTRAINT DF_StudyShifts_Status DEFAULT (N'Active'),

        Note NVARCHAR(255) NULL,

        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_StudyShifts_CreatedAt DEFAULT (SYSUTCDATETIME()),

        IsDeleted BIT NOT NULL
            CONSTRAINT DF_StudyShifts_IsDeleted DEFAULT (0),

        DeletedAt DATETIME2 NULL,
        DeletedByUserId INT NULL,

        CONSTRAINT PK_StudyShifts
            PRIMARY KEY (StudyShiftId),

        CONSTRAINT FK_StudyShifts_Centers
            FOREIGN KEY (CenterId) REFERENCES dbo.Centers(CenterId),

        CONSTRAINT FK_StudyShifts_DeletedByUser
            FOREIGN KEY (DeletedByUserId) REFERENCES dbo.Users(UserId),

        CONSTRAINT CK_StudyShifts_TimeRange
            CHECK (EndTime > StartTime),

        CONSTRAINT CK_StudyShifts_Status
            CHECK (Status IN (N'Active', N'Inactive'))
    );
END;
GO

/* Mã ca không trùng trong cùng trung tâm nếu chưa xóa */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.StudyShifts')
      AND name = N'UX_StudyShifts_CenterId_ShiftCode_NotDeleted'
)
BEGIN
    CREATE UNIQUE INDEX UX_StudyShifts_CenterId_ShiftCode_NotDeleted
    ON dbo.StudyShifts(CenterId, ShiftCode)
    WHERE IsDeleted = 0;
END;
GO

/* Không cho tạo trùng khung giờ trong cùng trung tâm nếu chưa xóa */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.StudyShifts')
      AND name = N'UX_StudyShifts_CenterId_Time_NotDeleted'
)
BEGIN
    CREATE UNIQUE INDEX UX_StudyShifts_CenterId_Time_NotDeleted
    ON dbo.StudyShifts(CenterId, StartTime, EndTime)
    WHERE IsDeleted = 0;
END;
GO

/* Tối ưu filter danh sách */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.StudyShifts')
      AND name = N'IX_StudyShifts_CenterId_IsDeleted_Status'
)
BEGIN
    CREATE INDEX IX_StudyShifts_CenterId_IsDeleted_Status
    ON dbo.StudyShifts(CenterId, IsDeleted, Status, StudyShiftId DESC);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.StudyShifts')
      AND name = N'IX_StudyShifts_CenterId_IsDeleted_Search'
)
BEGIN
    CREATE INDEX IX_StudyShifts_CenterId_IsDeleted_Search
    ON dbo.StudyShifts(CenterId, IsDeleted, ShiftCode, ShiftName);
END;
GO

/* Seed ca học mẫu nếu trung tâm chưa có ca nào */
INSERT INTO dbo.StudyShifts
    (CenterId, ShiftCode, ShiftName, StartTime, EndTime, Status, Note)
SELECT
    c.CenterId,
    v.ShiftCode,
    v.ShiftName,
    v.StartTime,
    v.EndTime,
    N'Active',
    N'Ca học mẫu'
FROM dbo.Centers c
CROSS APPLY
(
    VALUES
        (N'CA01', N'Ca sáng 1', CAST('07:00' AS TIME(0)), CAST('09:00' AS TIME(0))),
        (N'CA02', N'Ca sáng 2', CAST('09:00' AS TIME(0)), CAST('11:00' AS TIME(0))),
        (N'CA03', N'Ca chiều 1', CAST('13:30' AS TIME(0)), CAST('15:30' AS TIME(0))),
        (N'CA04', N'Ca chiều 2', CAST('15:30' AS TIME(0)), CAST('17:30' AS TIME(0))),
        (N'CA05', N'Ca tối 1', CAST('18:00' AS TIME(0)), CAST('20:00' AS TIME(0))),
        (N'CA06', N'Ca tối 2', CAST('19:00' AS TIME(0)), CAST('21:00' AS TIME(0)))
) v(ShiftCode, ShiftName, StartTime, EndTime)
WHERE c.Status = N'Active'
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.StudyShifts s
      WHERE s.CenterId = c.CenterId
        AND s.IsDeleted = 0
  );
GO

/* View phục vụ màn hình danh sách ca học */
IF OBJECT_ID(N'dbo.vw_StudyShiftOverview', N'V') IS NOT NULL
BEGIN
    DROP VIEW dbo.vw_StudyShiftOverview;
END;
GO

CREATE VIEW dbo.vw_StudyShiftOverview AS
SELECT
    s.StudyShiftId,
    s.CenterId,
    s.ShiftCode,
    s.ShiftName,
    s.StartTime,
    s.EndTime,
    s.Status,
    s.Note,
    s.CreatedAt,
    s.IsDeleted,
    COUNT(DISTINCT CASE
        WHEN c.Status = N'Active' THEN c.ClassId
    END) AS ActiveClassCount,
    COUNT(DISTINCT c.ClassId) AS TotalClassCount
FROM dbo.StudyShifts s
LEFT JOIN dbo.Classes c
    ON c.CenterId = s.CenterId
LEFT JOIN dbo.ClassSchedules cs
    ON cs.ClassId = c.ClassId
   AND cs.StartTime = s.StartTime
   AND cs.EndTime = s.EndTime
WHERE s.IsDeleted = 0
GROUP BY
    s.StudyShiftId,
    s.CenterId,
    s.ShiftCode,
    s.ShiftName,
    s.StartTime,
    s.EndTime,
    s.Status,
    s.Note,
    s.CreatedAt,
    s.IsDeleted;
GO

/* Kiểm tra */
SELECT
    StudyShiftId,
    CenterId,
    ShiftCode,
    ShiftName,
    StartTime,
    EndTime,
    Status,
    IsDeleted
FROM dbo.StudyShifts
ORDER BY CenterId, StartTime;
GO

USE EduBridgeDB;
GO

/* Cho phep nhieu ca hoc dung cung khung gio trong cung trung tam.
   Ma ca van unique theo CenterId + ShiftCode khi IsDeleted = 0. */

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.StudyShifts')
      AND name = N'UX_StudyShifts_CenterId_Time_NotDeleted'
)
BEGIN
    DROP INDEX UX_StudyShifts_CenterId_Time_NotDeleted
    ON dbo.StudyShifts;
END;
GO

SELECT
    StudyShiftId,
    CenterId,
    ShiftCode,
    ShiftName,
    StartTime,
    EndTime,
    Status,
    IsDeleted
FROM dbo.StudyShifts
ORDER BY CenterId, StartTime, StudyShiftId;
GO


USE EduBridgeDB;
GO

CREATE OR ALTER VIEW dbo.vw_StudyShiftOverview AS
SELECT
    s.StudyShiftId,
    s.CenterId,
    s.ShiftCode,
    s.ShiftName,
    s.StartTime,
    s.EndTime,
    s.Status,
    s.Note,
    s.CreatedAt,
    s.IsDeleted,
    COUNT(DISTINCT CASE
        WHEN c.Status = N'Active' THEN c.ClassId
    END) AS ActiveClassCount,
    COUNT(DISTINCT c.ClassId) AS TotalClassCount
FROM dbo.StudyShifts s
LEFT JOIN dbo.ClassSchedules cs
    ON cs.StartTime = s.StartTime
   AND cs.EndTime = s.EndTime
LEFT JOIN dbo.Classes c
    ON c.ClassId = cs.ClassId
   AND c.CenterId = s.CenterId
WHERE s.IsDeleted = 0
GROUP BY
    s.StudyShiftId,
    s.CenterId,
    s.ShiftCode,
    s.ShiftName,
    s.StartTime,
    s.EndTime,
    s.Status,
    s.Note,
    s.CreatedAt,
    s.IsDeleted;
GO