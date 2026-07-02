USE [EduBridgeDB];
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
