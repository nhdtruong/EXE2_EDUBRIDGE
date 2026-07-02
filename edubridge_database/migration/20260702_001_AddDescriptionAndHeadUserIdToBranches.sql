USE EduBridgeDB
GO

-- ============================================================
-- Migration: 20260702_001_AddDescriptionAndHeadUserIdToBranches
-- Mô tả    : Bổ sung 2 cột còn thiếu vào bảng Branches:
--              1. Description  – Mô tả cơ sở (NVARCHAR(500), NULL)
--              2. HeadUserId   – Trưởng cơ sở (FK → Users.UserId, NULL)
-- Tác giả  : EduBridge Dev Team
-- Ngày tạo : 2026-07-02
-- ============================================================

-- -------------------------------------------------------
-- IMPACT ANALYSIS
-- -------------------------------------------------------
-- Bảng ảnh hưởng  : Branches
-- Entity ảnh hưởng: Models/Branch.cs  → cần regenerate sau khi chạy script
-- Service         : Không ảnh hưởng hiện tại (2 cột mới, nullable)
-- API             : Không ảnh hưởng hiện tại
-- Razor Pages     : Pages/SystemAdmin/Branches.cshtml sẽ đọc được 2 cột này
-- Dữ liệu hiện tại: An toàn – cả 2 cột đều NULL, không phá vỡ bản ghi cũ
-- -------------------------------------------------------


-- -------------------------------------------------------
-- STEP 1: Thêm cột Description
-- -------------------------------------------------------
IF COL_LENGTH(N'dbo.Branches', N'Description') IS NULL
BEGIN
    ALTER TABLE dbo.Branches
    ADD Description NVARCHAR(500) NULL;

    PRINT 'Column [Description] added to [Branches].';
END
ELSE
BEGIN
    PRINT 'Column [Description] already exists in [Branches]. Skipped.';
END;
GO


-- -------------------------------------------------------
-- STEP 2: Thêm cột HeadUserId (FK → Users.UserId)
-- -------------------------------------------------------
IF COL_LENGTH(N'dbo.Branches', N'HeadUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Branches
    ADD HeadUserId INT NULL;

    PRINT 'Column [HeadUserId] added to [Branches].';
END
ELSE
BEGIN
    PRINT 'Column [HeadUserId] already exists in [Branches]. Skipped.';
END;
GO


-- -------------------------------------------------------
-- STEP 3: Tạo Foreign Key Branches.HeadUserId → Users.UserId
--         (chỉ tạo nếu chưa tồn tại)
-- -------------------------------------------------------
IF NOT EXISTS (
    SELECT 1
    FROM   sys.foreign_keys
    WHERE  name = N'FK_Branches_HeadUser'
      AND  parent_object_id = OBJECT_ID(N'dbo.Branches')
)
BEGIN
    ALTER TABLE dbo.Branches
    ADD CONSTRAINT FK_Branches_HeadUser
        FOREIGN KEY (HeadUserId)
        REFERENCES dbo.Users (UserId)
        ON UPDATE NO ACTION
        ON DELETE SET NULL;

    PRINT 'Foreign key [FK_Branches_HeadUser] created.';
END
ELSE
BEGIN
    PRINT 'Foreign key [FK_Branches_HeadUser] already exists. Skipped.';
END;
GO


-- -------------------------------------------------------
-- STEP 4: Tạo Index cho HeadUserId (hỗ trợ JOIN nhanh)
-- -------------------------------------------------------
IF NOT EXISTS (
    SELECT 1
    FROM   sys.indexes
    WHERE  name = N'IX_Branches_HeadUserId'
      AND  object_id = OBJECT_ID(N'dbo.Branches')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Branches_HeadUserId
        ON dbo.Branches (HeadUserId)
        WHERE HeadUserId IS NOT NULL;

    PRINT 'Index [IX_Branches_HeadUserId] created.';
END
ELSE
BEGIN
    PRINT 'Index [IX_Branches_HeadUserId] already exists. Skipped.';
END;
GO


-- -------------------------------------------------------
-- VERIFY – Kiểm tra kết quả sau khi chạy
-- -------------------------------------------------------
SELECT
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.IS_NULLABLE
FROM   INFORMATION_SCHEMA.COLUMNS c
WHERE  c.TABLE_NAME   = 'Branches'
  AND  c.COLUMN_NAME IN ('Description', 'HeadUserId')
ORDER BY c.COLUMN_NAME;
GO
