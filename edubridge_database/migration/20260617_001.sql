USE EduBridgeDB;
GO
-- 1. Thêm cột StaffCode vào bảng CenterUsers
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CenterUsers]') AND name = 'StaffCode')
BEGIN
    ALTER TABLE CenterUsers 
    ADD StaffCode NVARCHAR(30) NULL;
END
GO

-- 1. Xóa bỏ Unique Index cũ nếu tồn tại
IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[CenterUsers]') AND name = 'UX_CenterUsers_CenterId_StaffCode')
BEGIN
    DROP INDEX UX_CenterUsers_CenterId_StaffCode ON CenterUsers;
END
GO

-- 2. Tạo lại Index mới (không Unique) để đảm bảo hiệu suất truy vấn
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[CenterUsers]') AND name = 'IX_CenterUsers_CenterId_StaffCode')
BEGIN
    CREATE NONCLUSTERED INDEX IX_CenterUsers_CenterId_StaffCode 
    ON CenterUsers (CenterId, StaffCode);
END
GO
