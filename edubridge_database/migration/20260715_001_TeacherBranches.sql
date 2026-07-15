-- 1. Tạo bảng trung gian TeacherBranches
CREATE TABLE TeacherBranches (
    TeacherId INT NOT NULL,
    BranchId INT NOT NULL,
    CONSTRAINT PK_TeacherBranches PRIMARY KEY (TeacherId, BranchId),
    CONSTRAINT FK_TeacherBranches_Teachers FOREIGN KEY (TeacherId) REFERENCES Teachers(TeacherId) ON DELETE CASCADE,
    CONSTRAINT FK_TeacherBranches_Branches FOREIGN KEY (BranchId) REFERENCES Branches(BranchId) ON DELETE CASCADE
);
GO

-- 2. Migrate dữ liệu cũ từ Teachers.BranchId sang TeacherBranches
INSERT INTO TeacherBranches (TeacherId, BranchId)
SELECT TeacherId, BranchId
FROM Teachers
WHERE BranchId IS NOT NULL;
GO

-- 3. Xóa cột BranchId cũ (Bạn có thể bỏ qua bước này nếu muốn giữ lại cột cho an toàn)
-- Nếu có Khóa ngoại, phải tìm và xóa Khóa ngoại trước. Ví dụ:
-- ALTER TABLE Teachers DROP CONSTRAINT FK_Teachers_Branches;
-- ALTER TABLE Teachers DROP COLUMN BranchId;
-- GO
