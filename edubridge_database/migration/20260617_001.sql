USE EduBridgeDB;
GO
-- 1. Thêm cột StaffCode vào bảng CenterUsers
ALTER TABLE CenterUsers 
ADD StaffCode NVARCHAR(30) NULL;
GO

-- 2. Tạo Unique Index đảm bảo mã nhân sự không được trùng nhau trong cùng một trung tâm
CREATE UNIQUE NONCLUSTERED INDEX UX_CenterUsers_CenterId_StaffCode 
ON CenterUsers(CenterId, StaffCode) 
WHERE StaffCode IS NOT NULL;
GO

-- 1. Xóa bỏ Unique Index cũ
DROP INDEX UX_CenterUsers_CenterId_StaffCode ON CenterUsers;

-- 2. Tạo lại Index mới (không Unique) để đảm bảo hiệu suất truy vấn
CREATE NONCLUSTERED INDEX IX_CenterUsers_CenterId_StaffCode 
ON CenterUsers (CenterId, StaffCode);
