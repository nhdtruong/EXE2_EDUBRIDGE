USE EduBridgeDB;
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

/* =========================================================
   1. Courses: số buổi mặc định của môn học
   ========================================================= */

IF COL_LENGTH(N'dbo.Courses', N'TotalSessions') IS NULL
BEGIN
    ALTER TABLE dbo.Courses
    ADD TotalSessions INT NULL;
END;
GO

/*
    Backfill tạm:
    Giả định chương trình cũ trung bình học 2 buổi/tuần.
    Kiểm tra lại dữ liệu sau khi chạy và sửa thủ công nếu cần.
*/
IF COL_LENGTH(N'dbo.Courses', N'DurationWeeks') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        UPDATE dbo.Courses
        SET TotalSessions =
            CASE
                WHEN DurationWeeks > 0 THEN DurationWeeks * 2
                ELSE 24
            END
        WHERE TotalSessions IS NULL;
    ';
END
ELSE
BEGIN
    UPDATE dbo.Courses
    SET TotalSessions = 24
    WHERE TotalSessions IS NULL;
END;
GO

ALTER TABLE dbo.Courses
ALTER COLUMN TotalSessions INT NOT NULL;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    JOIN sys.columns col
        ON col.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Courses')
      AND col.name = N'TotalSessions'
)
BEGIN
    ALTER TABLE dbo.Courses
    ADD CONSTRAINT DF_Courses_TotalSessions
        DEFAULT 24 FOR TotalSessions;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.Courses')
      AND name = N'CK_Courses_TotalSessions'
)
BEGIN
    ALTER TABLE dbo.Courses
    ADD CONSTRAINT CK_Courses_TotalSessions
        CHECK (TotalSessions BETWEEN 1 AND 1000);
END;
GO

/* =========================================================
   2. Classes: snapshot số buổi áp dụng thật cho từng lớp
   ========================================================= */

IF COL_LENGTH(N'dbo.Classes', N'TotalSessions') IS NULL
BEGIN
    ALTER TABLE dbo.Classes
    ADD TotalSessions INT NULL;
END;
GO

/*
    Ưu tiên đếm lịch học thực tế trong khoảng StartDate - EndDate.
    DayOfWeek: 1=Thứ 2 ... 7=Chủ nhật.
*/
;WITH Numbers AS
(
    SELECT TOP (10000)
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS Number
    FROM sys.all_objects a
    CROSS JOIN sys.all_objects b
),
ScheduleCounts AS
(
    SELECT
        c.ClassId,
        COUNT(*) AS TotalScheduledSessions
    FROM dbo.Classes c
    JOIN Numbers n
        ON n.Number <= DATEDIFF(DAY, c.StartDate, c.EndDate)
    JOIN dbo.ClassSchedules cs
        ON cs.ClassId = c.ClassId
       AND cs.DayOfWeek =
            ((DATEDIFF(
                DAY,
                CONVERT(DATE, '19000101', 112),
                DATEADD(DAY, n.Number, c.StartDate)
            ) % 7 + 7) % 7) + 1
    GROUP BY c.ClassId
)
UPDATE c
SET TotalSessions =
    CASE
        WHEN sc.TotalScheduledSessions > 0
            THEN sc.TotalScheduledSessions
        ELSE co.TotalSessions
    END
FROM dbo.Classes c
JOIN dbo.Courses co
    ON co.CourseId = c.CourseId
LEFT JOIN ScheduleCounts sc
    ON sc.ClassId = c.ClassId
WHERE c.TotalSessions IS NULL;
GO

ALTER TABLE dbo.Classes
ALTER COLUMN TotalSessions INT NOT NULL;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.Classes')
      AND name = N'CK_Classes_TotalSessions'
)
BEGIN
    ALTER TABLE dbo.Classes
    ADD CONSTRAINT CK_Classes_TotalSessions
        CHECK (TotalSessions BETWEEN 1 AND 1000);
END;
GO

/* =========================================================
   3. Lessons: quản lý từng buổi học thật
   ========================================================= */

IF COL_LENGTH(N'dbo.Lessons', N'SessionNumber') IS NULL
BEGIN
    ALTER TABLE dbo.Lessons
    ADD SessionNumber INT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Lessons', N'ClassScheduleId') IS NULL
BEGIN
    ALTER TABLE dbo.Lessons
    ADD ClassScheduleId INT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Lessons', N'StartTime') IS NULL
BEGIN
    ALTER TABLE dbo.Lessons
    ADD StartTime TIME(0) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Lessons', N'EndTime') IS NULL
BEGIN
    ALTER TABLE dbo.Lessons
    ADD EndTime TIME(0) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Lessons', N'Status') IS NULL
BEGIN
    ALTER TABLE dbo.Lessons
    ADD Status NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Lessons_Status DEFAULT N'Scheduled';
END;
GO

/* Đánh số các lesson cũ */
;WITH NumberedLessons AS
(
    SELECT
        LessonId,
        ROW_NUMBER() OVER (
            PARTITION BY ClassId
            ORDER BY LessonDate, LessonId
        ) AS RowNum
    FROM dbo.Lessons
)
UPDATE l
SET SessionNumber = n.RowNum
FROM dbo.Lessons l
JOIN NumberedLessons n
    ON n.LessonId = l.LessonId
WHERE l.SessionNumber IS NULL;
GO

/* Chỉ gắn lịch tuần khi lesson cũ khớp duy nhất một ca */
;WITH MatchingSchedules AS
(
    SELECT
        l.LessonId,
        MAX(cs.ClassScheduleId) AS ClassScheduleId,
        COUNT(*) AS MatchCount
    FROM dbo.Lessons l
    JOIN dbo.ClassSchedules cs
        ON cs.ClassId = l.ClassId
       AND cs.DayOfWeek =
            ((DATEDIFF(
                DAY,
                CONVERT(DATE, '19000101', 112),
                l.LessonDate
            ) % 7 + 7) % 7) + 1
    GROUP BY l.LessonId
)
UPDATE l
SET ClassScheduleId = ms.ClassScheduleId
FROM dbo.Lessons l
JOIN MatchingSchedules ms
    ON ms.LessonId = l.LessonId
WHERE ms.MatchCount = 1
  AND l.ClassScheduleId IS NULL;
GO

UPDATE l
SET
    StartTime = cs.StartTime,
    EndTime = cs.EndTime
FROM dbo.Lessons l
JOIN dbo.ClassSchedules cs
    ON cs.ClassScheduleId = l.ClassScheduleId
WHERE l.StartTime IS NULL
   OR l.EndTime IS NULL;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.Lessons')
      AND name = N'FK_Lessons_ClassSchedules'
)
BEGIN
    ALTER TABLE dbo.Lessons
    ADD CONSTRAINT FK_Lessons_ClassSchedules
        FOREIGN KEY (ClassScheduleId)
        REFERENCES dbo.ClassSchedules(ClassScheduleId);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.Lessons')
      AND name = N'CK_Lessons_Status'
)
BEGIN
    ALTER TABLE dbo.Lessons
    ADD CONSTRAINT CK_Lessons_Status
        CHECK (Status IN (
            N'Scheduled',
            N'Completed',
            N'Cancelled',
            N'Rescheduled'
        ));
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.Lessons')
      AND name = N'CK_Lessons_TimeRange'
)
BEGIN
    ALTER TABLE dbo.Lessons
    ADD CONSTRAINT CK_Lessons_TimeRange
        CHECK (
            (StartTime IS NULL AND EndTime IS NULL)
            OR
            (StartTime IS NOT NULL AND EndTime IS NOT NULL AND EndTime > StartTime)
        );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Lessons')
      AND name = N'IX_Lessons_ClassId_Status_LessonDate'
)
BEGIN
    CREATE INDEX IX_Lessons_ClassId_Status_LessonDate
    ON dbo.Lessons(ClassId, Status, LessonDate);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Lessons')
      AND name = N'UX_Lessons_ClassId_SessionNumber'
)
BEGIN
    CREATE UNIQUE INDEX UX_Lessons_ClassId_SessionNumber
    ON dbo.Lessons(ClassId, SessionNumber)
    WHERE SessionNumber IS NOT NULL;
END;
GO

COMMIT TRANSACTION;
GO

/* =========================================================
   4. Kiểm tra sau migration
   ========================================================= */

IF COL_LENGTH(N'dbo.Courses', N'DurationWeeks') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        SELECT
            CourseId,
            CourseCode,
            CourseName,
            DurationWeeks AS OldDurationWeeks,
            TotalSessions
        FROM dbo.Courses
        ORDER BY CourseId;
    ';
END
ELSE
BEGIN
    SELECT
        CourseId,
        CourseCode,
        CourseName,
        TotalSessions
    FROM dbo.Courses
    ORDER BY CourseId;
END;
GO

SELECT
    c.ClassId,
    c.ClassCode,
    c.ClassName,
    c.StartDate,
    c.EndDate,
    c.TotalSessions,
    COUNT(l.LessonId) AS CurrentLessonCount
FROM dbo.Classes c
LEFT JOIN dbo.Lessons l
    ON l.ClassId = c.ClassId
GROUP BY
    c.ClassId,
    c.ClassCode,
    c.ClassName,
    c.StartDate,
    c.EndDate,
    c.TotalSessions
ORDER BY c.ClassId;
GO

SELECT
    LessonId,
    ClassId,
    SessionNumber,
    LessonDate,
    StartTime,
    EndTime,
    Status
FROM dbo.Lessons
ORDER BY ClassId, SessionNumber;
GO


DECLARE @DurationWeeksDefaultConstraint SYSNAME;
DECLARE @DropDurationWeeksDefaultSql NVARCHAR(MAX);

SELECT @DurationWeeksDefaultConstraint = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c
    ON c.default_object_id = dc.object_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Courses')
  AND c.name = N'DurationWeeks';

IF @DurationWeeksDefaultConstraint IS NOT NULL
BEGIN
    SET @DropDurationWeeksDefaultSql =
        N'ALTER TABLE dbo.Courses DROP CONSTRAINT ' +
        QUOTENAME(@DurationWeeksDefaultConstraint);

    EXEC sys.sp_executesql @DropDurationWeeksDefaultSql;
END;
GO

IF COL_LENGTH(N'dbo.Courses', N'DurationWeeks') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Courses
    DROP COLUMN DurationWeeks;
END;
GO
