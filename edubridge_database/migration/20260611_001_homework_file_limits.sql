USE EduBridgeDB;
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

-- Thêm cột AttachmentUrl vào bảng Homework nếu chưa có
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Homework') AND name = N'AttachmentUrl')
BEGIN
    ALTER TABLE dbo.Homework ADD AttachmentUrl NVARCHAR(500) NULL;
END;
GO

-- Thêm cột SubmissionFileUrl vào bảng HomeworkSubmissions nếu chưa có
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.HomeworkSubmissions') AND name = N'SubmissionFileUrl')
BEGIN
    ALTER TABLE dbo.HomeworkSubmissions ADD SubmissionFileUrl NVARCHAR(500) NULL;
END;
GO

COMMIT TRANSACTION;
GO
