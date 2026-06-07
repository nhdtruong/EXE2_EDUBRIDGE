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

    CREATE INDEX IX_HomeworkSubmissions_HomeworkId ON dbo.HomeworkSubmissions(HomeworkId);
    CREATE INDEX IX_HomeworkSubmissions_StudentId ON dbo.HomeworkSubmissions(StudentId);
END;
GO

/* =========================================================
   2. Chèn dữ liệu mẫu cho bài tập HomeworkId = 1
   ========================================================= */
IF EXISTS (SELECT 1 FROM dbo.Homework WHERE HomeworkId = 1)
BEGIN
    -- Chỉ chèn nếu chưa có dữ liệu mẫu
    IF NOT EXISTS (SELECT 1 FROM dbo.HomeworkSubmissions WHERE HomeworkId = 1)
    BEGIN
        -- STD001 (StudentId = 1): Đã nộp và đã chấm
        INSERT INTO dbo.HomeworkSubmissions (HomeworkId, StudentId, SubmissionContent, SubmittedAt, Score, Feedback, Status)
        VALUES (1, 1, N'Hello teacher, here is my voice message.', DATEADD(day, -2, GETDATE()), 9.0, N'Phát âm rõ ràng, trôi chảy!', N'Graded');

        -- STD002 (StudentId = 2): Đã nộp nhưng chưa chấm (Pending)
        INSERT INTO dbo.HomeworkSubmissions (HomeworkId, StudentId, SubmissionContent, SubmittedAt, Score, Feedback, Status)
        VALUES (1, 2, N'I have finished the homework video link: drive.google.com/xyz', DATEADD(day, -1, GETDATE()), NULL, NULL, N'Submitted');
    END;
END;
GO

COMMIT TRANSACTION;
GO
