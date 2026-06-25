USE EduBridgeDB;
GO
-- Thêm cột Logo vào bảng Centers
ALTER TABLE Centers
ADD Logo NVARCHAR(500) NULL;


USE EduBridgeDB;
GO

-- 1. Thêm cột (Tạm thời cho phép NULL để update data cũ)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Centers]') AND name = 'CenterCode')
BEGIN
    ALTER TABLE [dbo].[Centers] ADD [CenterCode] NVARCHAR(50);
END
GO

-- 2. Đổ dữ liệu mẫu cho các Center cũ (Ví dụ: CENTER-1)
UPDATE [dbo].[Centers] SET [CenterCode] = 'CENTER-' + CAST([CenterId] AS NVARCHAR(50)) WHERE [CenterCode] IS NULL;
GO

-- 3. Đặt thành NOT NULL
ALTER TABLE [dbo].[Centers] ALTER COLUMN [CenterCode] NVARCHAR(50) NOT NULL;
GO

-- 4. Thêm ràng buộc UNIQUE
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Centers_CenterCode')
BEGIN
    ALTER TABLE [dbo].[Centers] ADD CONSTRAINT [UQ_Centers_CenterCode] UNIQUE ([CenterCode]);
END
GO
