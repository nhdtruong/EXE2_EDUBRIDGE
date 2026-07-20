USE [EduBridgeDB];
GO

-- Xóa Unique Index cũ
IF EXISTS (SELECT name FROM sys.indexes WHERE name = N'UX_CenterUsers_CenterId_StaffCode')
BEGIN
    DROP INDEX [UX_CenterUsers_CenterId_StaffCode] ON [dbo].[CenterUsers];
END
GO

-- Tạo lại Index Non-Unique mới
CREATE NONCLUSTERED INDEX [IX_CenterUsers_CenterId_StaffCode] ON [dbo].[CenterUsers]
(
    [CenterId] ASC,
    [StaffCode] ASC
)
WHERE ([StaffCode] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY];
GO
