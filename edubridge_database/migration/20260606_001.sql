USE EduBridgeDB;
GO

SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    /* Không được đổi NOT NULL nếu vẫn còn lớp chưa có phòng */
    IF EXISTS (
        SELECT 1
        FROM dbo.Classes
        WHERE RoomId IS NULL
    )
    BEGIN
        PRINT N'Bỏ qua bước bắt buộc RoomId trong migration 20260606_001 vì vẫn còn lớp chưa được gán phòng.';
    END
    ELSE
    BEGIN
        /* Xóa index đang phụ thuộc RoomId */
        IF EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.Classes')
              AND name = N'IX_Classes_RoomId'
        )
        BEGIN
            DROP INDEX IX_Classes_RoomId
            ON dbo.Classes;
        END;

        /* Xóa index kiểm tra phòng nếu đã tồn tại */
        IF EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.Classes')
              AND name = N'IX_Classes_Center_Room_Status'
        )
        BEGIN
            DROP INDEX IX_Classes_Center_Room_Status
            ON dbo.Classes;
        END;

        /* Bắt buộc lớp phải có phòng */
        ALTER TABLE dbo.Classes
        ALTER COLUMN RoomId INT NOT NULL;

        /* Tạo lại index FK RoomId */
        CREATE INDEX IX_Classes_RoomId
        ON dbo.Classes(RoomId);

        /* Index hỗ trợ kiểm tra lịch phòng */
        CREATE INDEX IX_Classes_Center_Room_Status
        ON dbo.Classes (
            CenterId,
            RoomId,
            Status,
            ClassId
        )
        INCLUDE (
            TeacherId,
            StartDate,
            EndDate
        );
    END;

    /* Index hỗ trợ kiểm tra lịch giáo viên */
    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.Classes')
          AND name = N'IX_Classes_Center_Teacher_Status'
    )
    BEGIN
        CREATE INDEX IX_Classes_Center_Teacher_Status
        ON dbo.Classes (
            CenterId,
            TeacherId,
            Status,
            ClassId
        )
        INCLUDE (
            RoomId,
            StartDate,
            EndDate
        );
    END;

    /* Index hỗ trợ kiểm tra Lesson trùng giờ */
    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.Lessons')
          AND name = N'IX_Lessons_Date_Time_Status_Class'
    )
    BEGIN
        CREATE INDEX IX_Lessons_Date_Time_Status_Class
        ON dbo.Lessons (
            LessonDate,
            StartTime,
            EndTime,
            Status,
            ClassId
        )
        WHERE StartTime IS NOT NULL
          AND EndTime IS NOT NULL;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

SELECT
    c.name AS ColumnName,
    c.is_nullable AS IsNullable
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(N'dbo.Classes')
  AND c.name = N'RoomId';

SELECT
    name,
    is_unique
FROM sys.indexes
WHERE object_id IN (
    OBJECT_ID(N'dbo.Classes'),
    OBJECT_ID(N'dbo.Lessons')
)
AND name IN (
    N'IX_Classes_RoomId',
    N'IX_Classes_Center_Room_Status',
    N'IX_Classes_Center_Teacher_Status',
    N'IX_Lessons_Date_Time_Status_Class'
);

USE EduBridgeDB;
GO

SET XACT_ABORT ON;
GO

/* =========================================================
   1. Thêm các cột
   ========================================================= */

IF COL_LENGTH(N'dbo.Classes', N'UpdatedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Classes
    ADD UpdatedAt DATETIME2(0) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Classes', N'UpdatedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Classes
    ADD UpdatedByUserId INT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Classes', N'ClosedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Classes
    ADD ClosedAt DATETIME2(0) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Classes', N'ClosedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Classes
    ADD ClosedByUserId INT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Classes', N'IsDeleted') IS NULL
BEGIN
    ALTER TABLE dbo.Classes
    ADD IsDeleted BIT NOT NULL
        CONSTRAINT DF_Classes_IsDeleted DEFAULT (0)
        WITH VALUES;
END;
GO

IF COL_LENGTH(N'dbo.Classes', N'DeletedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Classes
    ADD DeletedAt DATETIME2(0) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Classes', N'DeletedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Classes
    ADD DeletedByUserId INT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Classes', N'RowVersion') IS NULL
BEGIN
    ALTER TABLE dbo.Classes
    ADD RowVersion ROWVERSION NOT NULL;
END;
GO


/* =========================================================
   2. Backfill audit cho lớp Closed hiện tại
   ========================================================= */

UPDATE dbo.Classes
SET ClosedAt = SYSDATETIME()
WHERE Status = N'Closed'
  AND ClosedAt IS NULL;
GO


/* =========================================================
   3. Foreign keys audit
   ========================================================= */

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_Classes_UpdatedByUser'
)
BEGIN
    EXEC(N'
        ALTER TABLE dbo.Classes
        ADD CONSTRAINT FK_Classes_UpdatedByUser
        FOREIGN KEY (UpdatedByUserId)
        REFERENCES dbo.Users(UserId);
    ');
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_Classes_ClosedByUser'
)
BEGIN
    EXEC(N'
        ALTER TABLE dbo.Classes
        ADD CONSTRAINT FK_Classes_ClosedByUser
        FOREIGN KEY (ClosedByUserId)
        REFERENCES dbo.Users(UserId);
    ');
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_Classes_DeletedByUser'
)
BEGIN
    EXEC(N'
        ALTER TABLE dbo.Classes
        ADD CONSTRAINT FK_Classes_DeletedByUser
        FOREIGN KEY (DeletedByUserId)
        REFERENCES dbo.Users(UserId);
    ');
END;
GO


/* =========================================================
   4. Check constraints
   ========================================================= */

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_Classes_SoftDelete'
      AND parent_object_id = OBJECT_ID(N'dbo.Classes')
)
BEGIN
    EXEC(N'
        ALTER TABLE dbo.Classes
        ADD CONSTRAINT CK_Classes_SoftDelete
        CHECK (
            (
                IsDeleted = 0
                AND DeletedAt IS NULL
                AND DeletedByUserId IS NULL
            )
            OR
            (
                IsDeleted = 1
                AND DeletedAt IS NOT NULL
                AND DeletedByUserId IS NOT NULL
            )
        );
    ');
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_Classes_ClosedAudit'
      AND parent_object_id = OBJECT_ID(N'dbo.Classes')
)
BEGIN
    EXEC(N'
        ALTER TABLE dbo.Classes
        ADD CONSTRAINT CK_Classes_ClosedAudit
        CHECK (
            Status <> N''Closed''
            OR ClosedAt IS NOT NULL
        );
    ');
END;
GO


/* =========================================================
   5. Index danh sách lớp
   ========================================================= */

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Classes')
      AND name = N'IX_Classes_Center_IsDeleted_Status_ClassId'
)
BEGIN
    EXEC(N'
        CREATE INDEX IX_Classes_Center_IsDeleted_Status_ClassId
        ON dbo.Classes (
            CenterId,
            IsDeleted,
            Status,
            ClassId DESC
        )
        INCLUDE (
            ClassCode,
            ClassName,
            CourseId,
            TeacherId,
            RoomId,
            StartDate,
            EndDate,
            TotalSessions
        );
    ');
END;
GO


/* =========================================================
   6. Index kiểm tra lịch giáo viên
   ========================================================= */

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Classes')
      AND name = N'IX_Classes_Center_Teacher_Active'
)
BEGIN
    EXEC(N'
        CREATE INDEX IX_Classes_Center_Teacher_Active
        ON dbo.Classes (
            CenterId,
            TeacherId,
            IsDeleted,
            Status,
            ClassId
        )
        INCLUDE (
            RoomId,
            StartDate,
            EndDate
        );
    ');
END;
GO


/* =========================================================
   7. Index kiểm tra lịch phòng
   ========================================================= */

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Classes')
      AND name = N'IX_Classes_Center_Room_Active'
)
BEGIN
    EXEC(N'
        CREATE INDEX IX_Classes_Center_Room_Active
        ON dbo.Classes (
            CenterId,
            RoomId,
            IsDeleted,
            Status,
            ClassId
        )
        INCLUDE (
            TeacherId,
            StartDate,
            EndDate
        );
    ');
END;
GO


/* =========================================================
   8. Index audit
   ========================================================= */

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Classes')
      AND name = N'IX_Classes_DeletedByUserId'
)
BEGIN
    EXEC(N'
        CREATE INDEX IX_Classes_DeletedByUserId
        ON dbo.Classes(DeletedByUserId)
        WHERE DeletedByUserId IS NOT NULL;
    ');
END;
GO


/* =========================================================
   9. Kiểm tra kết quả
   ========================================================= */

SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = N'dbo'
  AND TABLE_NAME = N'Classes'
ORDER BY ORDINAL_POSITION;
GO

SELECT
    name AS ConstraintName,
    definition
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID(N'dbo.Classes');
GO

SELECT
    name AS IndexName,
    is_unique
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'dbo.Classes')
ORDER BY name;
GO

USE EduBridgeDB;
GO

SET XACT_ABORT ON;
GO

/* =========================================================
   1. Bổ sung thông tin quản lý Enrollment
   ========================================================= */

IF COL_LENGTH(N'dbo.Enrollments', N'StatusChangedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Enrollments
    ADD StatusChangedAt DATETIME2(0) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Enrollments', N'UpdatedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Enrollments
    ADD UpdatedByUserId INT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Enrollments', N'Note') IS NULL
BEGIN
    ALTER TABLE dbo.Enrollments
    ADD Note NVARCHAR(500) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Enrollments', N'RowVersion') IS NULL
BEGIN
    ALTER TABLE dbo.Enrollments
    ADD RowVersion ROWVERSION NOT NULL;
END;
GO


/* =========================================================
   2. Chuẩn hóa dữ liệu Enrollment hiện tại
   ========================================================= */

UPDATE dbo.Enrollments
SET StatusChangedAt =
    CAST(EnrollDate AS DATETIME2(0))
WHERE StatusChangedAt IS NULL;
GO

ALTER TABLE dbo.Enrollments
ALTER COLUMN StatusChangedAt DATETIME2(0) NOT NULL;
GO


/* =========================================================
   3. Kiểm tra và tạo lại Status Constraint
   ========================================================= */

DECLARE @StatusConstraintName SYSNAME;

SELECT TOP 1
    @StatusConstraintName = cc.name
FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID(N'dbo.Enrollments')
  AND cc.definition LIKE N'%Status%';

IF @StatusConstraintName IS NOT NULL
BEGIN
    DECLARE @DropEnrollmentStatusConstraintSql NVARCHAR(MAX) =
        N'ALTER TABLE dbo.Enrollments DROP CONSTRAINT ' + QUOTENAME(@StatusConstraintName);
    EXEC sp_executesql @DropEnrollmentStatusConstraintSql;
END;
GO

ALTER TABLE dbo.Enrollments
ADD CONSTRAINT CK_Enrollments_Status
CHECK (Status IN (N'Đang học', N'Bảo lưu', N'Đã nghỉ'));
GO


/* =========================================================
   4. Foreign key người cập nhật
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Enrollments_UpdatedByUser'
)
BEGIN
    ALTER TABLE dbo.Enrollments
    ADD CONSTRAINT FK_Enrollments_UpdatedByUser
        FOREIGN KEY (UpdatedByUserId)
        REFERENCES dbo.Users(UserId);
END;
GO


/* =========================================================
   5. Bảng lưu lịch sử thay đổi Enrollment
   ========================================================= */

IF OBJECT_ID(N'dbo.EnrollmentHistories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EnrollmentHistories
    (
        EnrollmentHistoryId BIGINT IDENTITY(1,1) NOT NULL,
        EnrollmentId INT NOT NULL,

        OldStatus NVARCHAR(20) NULL,
        NewStatus NVARCHAR(20) NOT NULL,

        ChangedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_EnrollmentHistories_ChangedAt
            DEFAULT SYSDATETIME(),

        ChangedByUserId INT NULL,
        Note NVARCHAR(500) NULL,

        CONSTRAINT PK_EnrollmentHistories
            PRIMARY KEY (EnrollmentHistoryId),

        CONSTRAINT FK_EnrollmentHistories_Enrollments
            FOREIGN KEY (EnrollmentId)
            REFERENCES dbo.Enrollments(EnrollmentId),

        CONSTRAINT FK_EnrollmentHistories_ChangedByUser
            FOREIGN KEY (ChangedByUserId)
            REFERENCES dbo.Users(UserId),

        CONSTRAINT CK_EnrollmentHistories_OldStatus
            CHECK (
                OldStatus IS NULL OR
                OldStatus IN (N'Đang học', N'Bảo lưu', N'Đã nghỉ')
            ),

        CONSTRAINT CK_EnrollmentHistories_NewStatus
            CHECK (
                NewStatus IN (N'Đang học', N'Bảo lưu', N'Đã nghỉ')
            )
    );
END;
GO


/* =========================================================
   6. Index phục vụ truy vấn production
   ========================================================= */

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Enrollments')
      AND name = N'IX_Enrollments_ClassId_Status_StudentId'
)
BEGIN
    CREATE INDEX IX_Enrollments_ClassId_Status_StudentId
    ON dbo.Enrollments(ClassId, Status, StudentId)
    INCLUDE (EnrollDate, StatusChangedAt);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Enrollments')
      AND name = N'IX_Enrollments_StudentId_Status_ClassId'
)
BEGIN
    CREATE INDEX IX_Enrollments_StudentId_Status_ClassId
    ON dbo.Enrollments(StudentId, Status, ClassId)
    INCLUDE (EnrollDate, StatusChangedAt);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.EnrollmentHistories')
      AND name = N'IX_EnrollmentHistories_EnrollmentId_ChangedAt'
)
BEGIN
    CREATE INDEX IX_EnrollmentHistories_EnrollmentId_ChangedAt
    ON dbo.EnrollmentHistories(EnrollmentId, ChangedAt DESC);
END;
GO


/* =========================================================
   7. Seed lịch sử ban đầu cho dữ liệu hiện tại
   ========================================================= */


/* =========================================================
   8. Kiểm tra kết quả
   ========================================================= */

SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = N'dbo'
  AND TABLE_NAME = N'Enrollments'
ORDER BY ORDINAL_POSITION;
GO

SELECT
    e.EnrollmentId,
    e.StudentId,
    e.ClassId,
    e.EnrollDate,
    e.Status,
    e.StatusChangedAt,
    e.UpdatedByUserId,
    e.Note
FROM dbo.Enrollments e
ORDER BY e.EnrollmentId;
GO

SELECT
    h.EnrollmentHistoryId,
    h.EnrollmentId,
    h.OldStatus,
    h.NewStatus,
    h.ChangedAt,
    h.ChangedByUserId,
    h.Note
FROM dbo.EnrollmentHistories h
ORDER BY h.EnrollmentHistoryId;
GO


USE EduBridgeDB;
GO

SET XACT_ABORT ON;
GO

DECLARE @ConstraintName SYSNAME;
DECLARE @Sql NVARCHAR(MAX);

SELECT TOP (1)
    @ConstraintName = cc.name
FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID(N'dbo.Enrollments')
  AND cc.name = N'CK_Enrollments_Status';

IF @ConstraintName IS NULL
BEGIN
    ALTER TABLE dbo.Enrollments WITH CHECK
    ADD CONSTRAINT CK_Enrollments_Status
    CHECK (Status IN (N'Đang học', N'Bảo lưu', N'Đã nghỉ'));

    ALTER TABLE dbo.Enrollments
    CHECK CONSTRAINT CK_Enrollments_Status;
END;
GO

SELECT
    cc.name AS ConstraintName,
    cc.definition,
    cc.is_disabled,
    cc.is_not_trusted
FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID(N'dbo.Enrollments');
GO
