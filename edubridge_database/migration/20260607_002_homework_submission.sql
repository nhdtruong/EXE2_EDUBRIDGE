USE EduBridgeDB;
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

/* =========================================================
   1. Tạo bảng HomeworkSubmissions
   ========================================================= */
IF OBJECT_ID(N'dbo.HomeworkSubmissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HomeworkSubmissions (
        SubmissionId INT IDENTITY(1,1) PRIMARY KEY,
        HomeworkId INT NOT NULL,
        StudentId INT NOT NULL,
        SubmissionContent NVARCHAR(MAX) NULL,
        SubmittedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        Score DECIMAL(5,2) NULL,
        Feedback NVARCHAR(500) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT N'Submitted', -- Submitted, Graded
        
        CONSTRAINT FK_HomeworkSubmissions_Homework
            FOREIGN KEY(HomeworkId)
            REFERENCES dbo.Homework(HomeworkId)
            ON DELETE CASCADE,
            
        CONSTRAINT FK_HomeworkSubmissions_Students
            FOREIGN KEY(StudentId)
            REFERENCES dbo.Students(StudentId),
            
        CONSTRAINT UQ_HomeworkSubmissions
            UNIQUE(HomeworkId, StudentId)
    );

END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.HomeworkSubmissions')
      AND name = N'IX_HomeworkSubmissions_HomeworkId'
)
BEGIN
    CREATE INDEX IX_HomeworkSubmissions_HomeworkId ON dbo.HomeworkSubmissions(HomeworkId);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.HomeworkSubmissions')
      AND name = N'IX_HomeworkSubmissions_StudentId'
)
BEGIN
    CREATE INDEX IX_HomeworkSubmissions_StudentId ON dbo.HomeworkSubmissions(StudentId);
END;
GO

/* =========================================================
   2. Chèn dữ liệu mẫu cho bài tập HomeworkId = 1
   ========================================================= */
PRINT N'Bo qua seed HomeworkSubmissions mau trong migration 20260607_002_homework_submission. Du lieu test duoc quan ly o script seed rieng.';
GO

COMMIT TRANSACTION;
GO
