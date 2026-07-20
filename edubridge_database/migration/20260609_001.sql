USE EduBridgeDB;
GO

-- Kiểm tra và thêm cột SettingsJson nếu chưa tồn tại
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Centers' AND COLUMN_NAME = 'SettingsJson'
)
BEGIN
    ALTER TABLE Centers
    ADD SettingsJson NVARCHAR(MAX) NULL;
    
    PRINT 'Da them cot SettingsJson vao bang Centers.';
END
ELSE
BEGIN
    PRINT 'Cot SettingsJson da ton tai trong bang Centers.';
END
GO


USE EduBridgeDB;
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DataProtectionKeys]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[DataProtectionKeys] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [FriendlyName] NVARCHAR(MAX) NULL,
    [Xml] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_DataProtectionKeys] PRIMARY KEY CLUSTERED ([Id] ASC)
);
END
GO