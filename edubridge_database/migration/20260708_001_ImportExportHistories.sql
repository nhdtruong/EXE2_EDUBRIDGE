USE [EduBridgeDB];
GO

IF OBJECT_ID(N'[dbo].[ImportExportHistories]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ImportExportHistories] (
        [HistoryId] INT IDENTITY(1,1) NOT NULL,
        [CenterId] INT NOT NULL,
        [UserId] INT NOT NULL,
        [Title] NVARCHAR(255) NOT NULL,
        [ActionType] NVARCHAR(50) NOT NULL,
        [EntityName] NVARCHAR(100) NOT NULL,
        [InputFileUrl] NVARCHAR(500) NULL,
        [ResultFileUrl] NVARCHAR(500) NULL,
        [Status] NVARCHAR(50) NOT NULL,
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_ImportExportHistories] PRIMARY KEY CLUSTERED ([HistoryId] ASC),
        CONSTRAINT [FK_ImportExportHistories_Centers] FOREIGN KEY ([CenterId]) REFERENCES [dbo].[Centers] ([CenterId]),
        CONSTRAINT [FK_ImportExportHistories_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
    );
END
GO
