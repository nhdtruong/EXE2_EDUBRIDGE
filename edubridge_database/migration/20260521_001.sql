
/* =========================================================
   Thêm mới điểm danh
========================================================= */
USE EduBridgeDB;
GO

DECLARE @ClassId INT;

SELECT @ClassId = ClassId
FROM Classes
WHERE ClassCode = 'CLS001';

IF @ClassId IS NULL
BEGIN
    PRINT N'Không tìm thấy lớp CLS001. Bỏ qua phần seed Lessons/Attendance mẫu trong migration 20260521_001.';
    GOTO SkipAttendanceSeed;
END;

DECLARE @LessonDates TABLE
(
    LessonDate DATE,
    LessonTitle NVARCHAR(200)
);

INSERT INTO @LessonDates (LessonDate, LessonTitle)
VALUES
('2026-05-15', N'Buổi học ngày 15/05'),
('2026-05-16', N'Buổi học ngày 16/05'),
('2026-05-17', N'Buổi học ngày 17/05'),
('2026-05-18', N'Buổi học ngày 18/05'),
('2026-05-19', N'Buổi học ngày 19/05'),
('2026-05-20', N'Buổi học ngày 20/05'),
('2026-05-21', N'Buổi học ngày 21/05');

INSERT INTO Lessons (ClassId, LessonTitle, LessonDate, LessonContent)
SELECT
    @ClassId,
    d.LessonTitle,
    d.LessonDate,
    N'Dữ liệu test biểu đồ điểm danh'
FROM @LessonDates d
WHERE NOT EXISTS
(
    SELECT 1
    FROM Lessons l
    WHERE l.ClassId = @ClassId
      AND l.LessonDate = d.LessonDate
);
GO

USE EduBridgeDB;
GO

DECLARE @ClassId INT;

SELECT @ClassId = ClassId
FROM Classes
WHERE ClassCode = 'CLS001';

;WITH TargetLessons AS
(
    SELECT LessonId, LessonDate
    FROM Lessons
    WHERE ClassId = @ClassId
      AND LessonDate BETWEEN '2026-05-15' AND '2026-05-21'
),
TargetStudents AS
(
    SELECT StudentId, StudentCode
    FROM Students
    WHERE StudentCode IN ('STD001', 'STD002')
),
AttendanceSeed AS
(
    SELECT l.LessonId, s.StudentId,
           CASE
               WHEN l.LessonDate IN ('2026-05-15', '2026-05-18', '2026-05-19') THEN N'Có mặt'
               WHEN l.LessonDate IN ('2026-05-16', '2026-05-20') AND s.StudentCode = 'STD001' THEN N'Có mặt'
               WHEN l.LessonDate IN ('2026-05-16', '2026-05-20') AND s.StudentCode = 'STD002' THEN N'Vắng'
               WHEN l.LessonDate IN ('2026-05-17', '2026-05-21') THEN N'Có mặt'
               ELSE N'Có mặt'
           END AS Status
    FROM TargetLessons l
    CROSS JOIN TargetStudents s
)
INSERT INTO Attendance (LessonId, StudentId, Status)
SELECT
    a.LessonId,
    a.StudentId,
    a.Status
FROM AttendanceSeed a
WHERE NOT EXISTS
(
    SELECT 1
    FROM Attendance existing
    WHERE existing.LessonId = a.LessonId
      AND existing.StudentId = a.StudentId
);
GO


SkipAttendanceSeed:
GO

/* =========================================================
   THÊM BẢNG INVOICES & PAYMENTS
========================================================= */
USE EduBridgeDB;
GO

CREATE TABLE Invoices (
    InvoiceId INT IDENTITY(1,1) PRIMARY KEY,
    StudentId INT NOT NULL,
    ClassId INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    FinalAmount AS (Amount - DiscountAmount) PERSISTED,
    DueDate DATE NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT N'Unpaid',
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Invoices_Students
        FOREIGN KEY (StudentId) REFERENCES Students(StudentId),

    CONSTRAINT FK_Invoices_Classes
        FOREIGN KEY (ClassId) REFERENCES Classes(ClassId),

    CONSTRAINT CK_Invoices_Status
        CHECK (Status IN (N'Unpaid', N'Partial', N'Paid', N'Cancelled'))
);

CREATE TABLE Payments (
    PaymentId INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceId INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PaidAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    PaymentMethod NVARCHAR(30) NULL,
    Note NVARCHAR(255) NULL,

    CONSTRAINT FK_Payments_Invoices
        FOREIGN KEY (InvoiceId) REFERENCES Invoices(InvoiceId),

    CONSTRAINT CK_Payments_Amount
        CHECK (Amount > 0)
);

CREATE INDEX IX_Invoices_StudentId ON Invoices(StudentId);
CREATE INDEX IX_Invoices_ClassId ON Invoices(ClassId);
CREATE INDEX IX_Invoices_Status ON Invoices(Status);
CREATE INDEX IX_Payments_InvoiceId ON Payments(InvoiceId);
CREATE INDEX IX_Payments_PaidAt ON Payments(PaidAt);
GO


CREATE VIEW vw_RevenueByPayment AS
SELECT
    c.CenterId,
    CAST(p.PaidAt AS DATE) AS PaidDate,
    YEAR(p.PaidAt) AS PaidYear,
    MONTH(p.PaidAt) AS PaidMonth,
    SUM(p.Amount) AS RevenueAmount
FROM Payments p
JOIN Invoices i ON i.InvoiceId = p.InvoiceId
JOIN Classes cl ON cl.ClassId = i.ClassId
JOIN Centers c ON c.CenterId = cl.CenterId
WHERE i.Status <> N'Cancelled'
GROUP BY
    c.CenterId,
    CAST(p.PaidAt AS DATE),
    YEAR(p.PaidAt),
    MONTH(p.PaidAt);
GO


/* =========================================================
   XÓA CỘT MAX STUDENT TRONG BẢNG CLASS
========================================================= */

USE EduBridgeDB;
GO

/* 1. Drop view phụ thuộc Classes */
IF OBJECT_ID(N'dbo.vw_ClassOverview', N'V') IS NOT NULL
BEGIN
    DROP VIEW dbo.vw_ClassOverview;
END;
GO

/* 2. Drop UNIQUE KEY constraint trên ClassCode */
DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql = @sql +
    N'ALTER TABLE dbo.Classes DROP CONSTRAINT ' + QUOTENAME(kc.name) + N';' + CHAR(13)
FROM sys.key_constraints kc
JOIN sys.index_columns ic
    ON ic.object_id = kc.parent_object_id
   AND ic.index_id = kc.unique_index_id
JOIN sys.columns c
    ON c.object_id = ic.object_id
   AND c.column_id = ic.column_id
WHERE kc.parent_object_id = OBJECT_ID(N'dbo.Classes')
  AND c.name = N'ClassCode'
  AND kc.type = N'UQ';

IF @sql <> N''
BEGIN
    PRINT @sql;
    EXEC sp_executesql @sql;
END;
GO

/* 3. Drop indexes thường trên ClassCode hoặc MaxStudents */
DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql = @sql +
    N'DROP INDEX ' + QUOTENAME(i.name) + N' ON dbo.Classes;' + CHAR(13)
FROM sys.indexes i
JOIN sys.index_columns ic
    ON ic.object_id = i.object_id
   AND ic.index_id = i.index_id
JOIN sys.columns c
    ON c.object_id = ic.object_id
   AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID(N'dbo.Classes')
  AND c.name IN (N'ClassCode', N'MaxStudents')
  AND i.is_primary_key = 0
  AND i.is_unique_constraint = 0
  AND i.name IS NOT NULL;

IF @sql <> N''
BEGIN
    PRINT @sql;
    EXEC sp_executesql @sql;
END;
GO

/* 4. Drop default/check constraints trên ClassCode hoặc MaxStudents */
DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql = @sql +
    N'ALTER TABLE dbo.Classes DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';' + CHAR(13)
FROM sys.default_constraints dc
JOIN sys.columns c
    ON c.object_id = dc.parent_object_id
   AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Classes')
  AND c.name IN (N'ClassCode', N'MaxStudents');

SELECT @sql = @sql +
    N'ALTER TABLE dbo.Classes DROP CONSTRAINT ' + QUOTENAME(cc.name) + N';' + CHAR(13)
FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID(N'dbo.Classes')
  AND (
      cc.definition LIKE N'%ClassCode%' OR
      cc.definition LIKE N'%MaxStudents%'
  );

IF @sql <> N''
BEGIN
    PRINT @sql;
    EXEC sp_executesql @sql;
END;
GO

/* 5. Drop columns */
IF COL_LENGTH(N'dbo.Classes', N'ClassCode') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Classes DROP COLUMN ClassCode;
END;
GO

IF COL_LENGTH(N'dbo.Classes', N'MaxStudents') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Classes DROP COLUMN MaxStudents;
END;
GO

/* 6. Tạo lại view không còn ClassCode và MaxStudents */
CREATE VIEW dbo.vw_ClassOverview AS
SELECT
    c.ClassId,
    c.ClassName,
    co.CourseName,
    ce.CenterName,
    u.FullName AS TeacherName,
    c.ScheduleText,
    c.Room,
    c.StartDate,
    c.EndDate,
    c.Status,
    COUNT(e.EnrollmentId) AS TotalStudents
FROM dbo.Classes c
JOIN dbo.Courses co
    ON co.CourseId = c.CourseId
JOIN dbo.Centers ce
    ON ce.CenterId = c.CenterId
JOIN dbo.Teachers t
    ON t.TeacherId = c.TeacherId
JOIN dbo.Users u
    ON u.UserId = t.UserId
LEFT JOIN dbo.Enrollments e
    ON e.ClassId = c.ClassId
   AND e.Status = N'Đang học'
GROUP BY
    c.ClassId,
    c.ClassName,
    co.CourseName,
    ce.CenterName,
    u.FullName,
    c.ScheduleText,
    c.Room,
    c.StartDate,
    c.EndDate,
    c.Status;
GO



USE EduBridgeDB;
GO

/* =========================================================
   1. ClassSchedules: tách lịch học khỏi ScheduleText
   ========================================================= */

IF OBJECT_ID(N'dbo.ClassSchedules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClassSchedules (
        ClassScheduleId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ClassId INT NOT NULL,
        DayOfWeek TINYINT NOT NULL, -- 1=Thứ 2 ... 7=Chủ nhật
        StartTime TIME(0) NOT NULL,
        EndTime TIME(0) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

        CONSTRAINT FK_ClassSchedules_Classes
            FOREIGN KEY (ClassId) REFERENCES dbo.Classes(ClassId),

        CONSTRAINT CK_ClassSchedules_DayOfWeek
            CHECK (DayOfWeek BETWEEN 1 AND 7),

        CONSTRAINT CK_ClassSchedules_TimeRange
            CHECK (EndTime > StartTime)
    );
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ClassSchedules')
      AND name = N'IX_ClassSchedules_ClassId'
)
BEGIN
    CREATE INDEX IX_ClassSchedules_ClassId
    ON dbo.ClassSchedules(ClassId);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ClassSchedules')
      AND name = N'UQ_ClassSchedules_Class_Day_Time'
)
BEGIN
    CREATE UNIQUE INDEX UQ_ClassSchedules_Class_Day_Time
    ON dbo.ClassSchedules(ClassId, DayOfWeek, StartTime, EndTime);
END;
GO


/* =========================================================
   2. ClassCodeCounters: sinh ClassCode an toàn
   Format gợi ý: CLS-{CenterId}-{yyyyMM}-{0001}
   ========================================================= */

IF OBJECT_ID(N'dbo.ClassCodeCounters', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClassCodeCounters (
        CenterId INT NOT NULL,
        YearMonth CHAR(6) NOT NULL,
        LastNumber INT NOT NULL,

        CONSTRAINT PK_ClassCodeCounters
            PRIMARY KEY (CenterId, YearMonth),

        CONSTRAINT FK_ClassCodeCounters_Centers
            FOREIGN KEY (CenterId) REFERENCES dbo.Centers(CenterId),

        CONSTRAINT CK_ClassCodeCounters_LastNumber
            CHECK (LastNumber >= 0)
    );
END;
GO


/* =========================================================
   3. Classes: đảm bảo ClassCode tồn tại + unique
   ========================================================= */

IF COL_LENGTH(N'dbo.Classes', N'ClassCode') IS NULL
BEGIN
    ALTER TABLE dbo.Classes
    ADD ClassCode NVARCHAR(30) NULL;
END;
GO

;WITH NumberedClasses AS
(
    SELECT
        ClassId,
        ROW_NUMBER() OVER (ORDER BY ClassId) AS RowNum
    FROM dbo.Classes
    WHERE ClassCode IS NULL OR LTRIM(RTRIM(ClassCode)) = N''
)
UPDATE c
SET ClassCode = CONCAT(N'CLS', RIGHT(CONCAT(N'0000', n.RowNum), 4))
FROM dbo.Classes c
JOIN NumberedClasses n
    ON n.ClassId = c.ClassId;
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Classes')
      AND name = N'ClassCode'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE dbo.Classes
    ALTER COLUMN ClassCode NVARCHAR(30) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Classes')
      AND name = N'IX_Classes_ClassCode'
)
BEGIN
    CREATE INDEX IX_Classes_ClassCode
    ON dbo.Classes(ClassCode);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.Classes')
      AND type = N'UQ'
      AND name = N'UQ_Classes_ClassCode'
)
BEGIN
    ALTER TABLE dbo.Classes
    ADD CONSTRAINT UQ_Classes_ClassCode UNIQUE (ClassCode);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Classes')
      AND name = N'IX_Classes_CenterId_ClassCode'
)
BEGIN
    CREATE INDEX IX_Classes_CenterId_ClassCode
    ON dbo.Classes(CenterId, ClassCode);
END;
GO


/* =========================================================
   4. Status check constraints
   ========================================================= */

/* Attendance: chuẩn hóa về 3 trạng thái Có mặt / Vắng / Muộn */
IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Attendance_Status'
      AND parent_object_id = OBJECT_ID(N'dbo.Attendance')
)
BEGIN
    ALTER TABLE dbo.Attendance
    DROP CONSTRAINT CK_Attendance_Status;
END;
GO

UPDATE dbo.Attendance
SET Status = N'Muộn'
WHERE Status IN (N'Đi muộn', N'Muộn');
GO

UPDATE dbo.Attendance
SET Status = N'Vắng'
WHERE Status IN (N'Có phép');
GO

UPDATE dbo.Attendance
SET Status = N'Vắng'
WHERE Status IS NULL
   OR LTRIM(RTRIM(Status)) = N''
   OR Status NOT IN (N'Có mặt', N'Vắng', N'Muộn');
GO

ALTER TABLE dbo.Attendance
ADD CONSTRAINT CK_Attendance_Status
CHECK (Status IN (N'Có mặt', N'Vắng', N'Muộn'));
GO


/* =========================================================
   5. Users.NormalizedPhoneNumber
   ========================================================= */

IF COL_LENGTH(N'dbo.Users', N'NormalizedPhoneNumber') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD NormalizedPhoneNumber NVARCHAR(20) NULL;
END;
GO

UPDATE dbo.Users
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

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Users')
      AND name = N'IX_Users_NormalizedPhoneNumber'
)
BEGIN
    CREATE INDEX IX_Users_NormalizedPhoneNumber
    ON dbo.Users(NormalizedPhoneNumber);
END;
GO


/* =========================================================
   6. Recreate vw_ClassOverview
   ========================================================= */

IF OBJECT_ID(N'dbo.vw_ClassOverview', N'V') IS NOT NULL
BEGIN
    DROP VIEW dbo.vw_ClassOverview;
END;
GO

CREATE VIEW dbo.vw_ClassOverview AS
SELECT
    c.ClassId,
    c.ClassCode,
    c.ClassName,
    co.CourseName,
    ce.CenterName,
    u.FullName AS TeacherName,
    c.ScheduleText,
    c.Room,
    c.StartDate,
    c.EndDate,
    c.Status,
    COUNT(e.EnrollmentId) AS TotalStudents
FROM dbo.Classes c
JOIN dbo.Courses co
    ON co.CourseId = c.CourseId
JOIN dbo.Centers ce
    ON ce.CenterId = c.CenterId
JOIN dbo.Teachers t
    ON t.TeacherId = c.TeacherId
JOIN dbo.Users u
    ON u.UserId = t.UserId
LEFT JOIN dbo.Enrollments e
    ON e.ClassId = c.ClassId
   AND e.Status = N'Đang học'
GROUP BY
    c.ClassId,
    c.ClassCode,
    c.ClassName,
    co.CourseName,
    ce.CenterName,
    u.FullName,
    c.ScheduleText,
    c.Room,
    c.StartDate,
    c.EndDate,
    c.Status;
GO


/* =========================================================
   7. Seed invoice/payment mẫu nếu đang trống
   ========================================================= */

IF NOT EXISTS (SELECT 1 FROM dbo.Invoices)
BEGIN
    INSERT INTO dbo.Invoices
        (StudentId, ClassId, Amount, DiscountAmount, DueDate, Status)
    SELECT TOP 1
        s.StudentId,
        c.ClassId,
        ISNULL(co.TuitionFee, 7000000),
        0,
        CAST(GETDATE() AS DATE),
        N'Paid'
    FROM dbo.Students s
    JOIN dbo.Enrollments e
        ON e.StudentId = s.StudentId
    JOIN dbo.Classes c
        ON c.ClassId = e.ClassId
    JOIN dbo.Courses co
        ON co.CourseId = c.CourseId
    WHERE e.Status = N'Đang học'
    ORDER BY s.StudentId;
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Payments)
BEGIN
    INSERT INTO dbo.Payments
        (InvoiceId, Amount, PaidAt, PaymentMethod, Note)
    SELECT TOP 1
        InvoiceId,
        FinalAmount,
        SYSDATETIME(),
        N'Cash',
        N'Dữ liệu mẫu thanh toán'
    FROM dbo.Invoices
    WHERE Status = N'Paid'
    ORDER BY InvoiceId;
END;
GO


/* =========================================================
   8. Kiểm tra nhanh
   ========================================================= */

SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN (
    N'Classes',
    N'ClassSchedules',
    N'ClassCodeCounters',
    N'Users'
)
ORDER BY TABLE_NAME, ORDINAL_POSITION;
GO

SELECT
    'Classes' AS TableName,
    COUNT(*) AS TotalRows
FROM dbo.Classes
UNION ALL
SELECT 'ClassSchedules', COUNT(*) FROM dbo.ClassSchedules
UNION ALL
SELECT 'ClassCodeCounters', COUNT(*) FROM dbo.ClassCodeCounters
UNION ALL
SELECT 'Invoices', COUNT(*) FROM dbo.Invoices
UNION ALL
SELECT 'Payments', COUNT(*) FROM dbo.Payments;
GO
