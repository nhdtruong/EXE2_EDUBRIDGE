USE EduBridgeDB;
GO

-- ========================================================
-- 0. XÓA DỮ LIỆU TEST CŨ KHÔNG HỢP LỆ
-- ========================================================
-- Không xóa dữ liệu production trong migration.
PRINT N'Bỏ qua bước xóa dữ liệu test cũ trong migration 20260608_001.';
GO

-- ========================================================
-- 1. THÊM TUITIONFEE VÀO BẢNG CLASSES
-- ========================================================
-- Kiểm tra xem cột TuitionFee đã tồn tại chưa để tránh lỗi nếu chạy lại
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Classes]') AND name = 'TuitionFee')
BEGIN
    ALTER TABLE Classes ADD TuitionFee DECIMAL(18,2) NULL;
END
GO

-- ========================================================
-- 2. CẬP NHẬT BẢNG INVOICES
-- ========================================================
IF COL_LENGTH(N'dbo.Invoices', N'CenterId') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices ADD CenterId INT NULL;
END
GO

IF COL_LENGTH(N'dbo.Invoices', N'InvoiceCode') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices ADD InvoiceCode NVARCHAR(30) NULL;
END
GO

IF COL_LENGTH(N'dbo.Invoices', N'EnrollmentId') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices ADD EnrollmentId INT NULL;
END
GO

IF COL_LENGTH(N'dbo.Invoices', N'Description') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices ADD Description NVARCHAR(500) NULL;
END
GO

IF COL_LENGTH(N'dbo.Invoices', N'DiscountNote') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices ADD DiscountNote NVARCHAR(500) NULL;
END
GO

IF COL_LENGTH(N'dbo.Invoices', N'CreatedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices ADD CreatedByUserId INT NULL;
END
GO

IF COL_LENGTH(N'dbo.Invoices', N'UpdatedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices ADD UpdatedAt DATETIME NULL;
END
GO

-- Thêm Foreign Keys cho Invoices
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Invoices_Centers]') AND parent_object_id = OBJECT_ID(N'[dbo].[Invoices]'))
ALTER TABLE Invoices ADD CONSTRAINT FK_Invoices_Centers FOREIGN KEY (CenterId) REFERENCES Centers(CenterId);
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Invoices_Enrollments]') AND parent_object_id = OBJECT_ID(N'[dbo].[Invoices]'))
ALTER TABLE Invoices ADD CONSTRAINT FK_Invoices_Enrollments FOREIGN KEY (EnrollmentId) REFERENCES Enrollments(EnrollmentId);
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Invoices_Users]') AND parent_object_id = OBJECT_ID(N'[dbo].[Invoices]'))
ALTER TABLE Invoices ADD CONSTRAINT FK_Invoices_Users FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId);
GO

-- Thêm Indexes cho Invoices
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Invoices]') AND name = N'IX_Invoices_CenterId_Status')
CREATE NONCLUSTERED INDEX IX_Invoices_CenterId_Status ON Invoices(CenterId, Status);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Invoices]') AND name = N'UX_Invoices_CenterId_InvoiceCode')
CREATE UNIQUE NONCLUSTERED INDEX UX_Invoices_CenterId_InvoiceCode ON Invoices(CenterId, InvoiceCode);
GO

-- ========================================================
-- 3. CẬP NHẬT BẢNG PAYMENTS
-- ========================================================
IF COL_LENGTH(N'dbo.Payments', N'CenterId') IS NULL
BEGIN
    ALTER TABLE dbo.Payments ADD CenterId INT NULL;
END
GO

IF COL_LENGTH(N'dbo.Payments', N'ReceivedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Payments ADD ReceivedByUserId INT NULL;
END
GO

IF COL_LENGTH(N'dbo.Payments', N'Status') IS NULL
BEGIN
    ALTER TABLE dbo.Payments ADD Status NVARCHAR(20) NOT NULL DEFAULT 'Confirmed';
END
GO

IF COL_LENGTH(N'dbo.Payments', N'TransactionReference') IS NULL
BEGIN
    ALTER TABLE dbo.Payments ADD TransactionReference NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH(N'dbo.Payments', N'CreatedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Payments ADD CreatedAt DATETIME NOT NULL DEFAULT SYSDATETIME();
END
GO

-- Thêm Foreign Keys cho Payments
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Payments_Centers]') AND parent_object_id = OBJECT_ID(N'[dbo].[Payments]'))
ALTER TABLE Payments ADD CONSTRAINT FK_Payments_Centers FOREIGN KEY (CenterId) REFERENCES Centers(CenterId);
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Payments_Users]') AND parent_object_id = OBJECT_ID(N'[dbo].[Payments]'))
ALTER TABLE Payments ADD CONSTRAINT FK_Payments_Users FOREIGN KEY (ReceivedByUserId) REFERENCES Users(UserId);
GO

-- Thêm Indexes cho Payments
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND name = N'IX_Payments_CenterId_Status')
CREATE NONCLUSTERED INDEX IX_Payments_CenterId_Status ON Payments(CenterId, Status);
GO

-- ========================================================
-- 4. TẠO BẢNG RECEIPTS (BIÊN LAI)
-- ========================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Receipts]') AND type in (N'U'))
BEGIN
    CREATE TABLE Receipts (
        ReceiptId INT IDENTITY(1,1) PRIMARY KEY,
        ReceiptNumber NVARCHAR(30) NOT NULL,
        PaymentId INT NOT NULL,
        CenterId INT NOT NULL,
        StudentName NVARCHAR(100) NOT NULL,
        ClassName NVARCHAR(150) NOT NULL,
        CourseName NVARCHAR(150) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        PaymentMethod NVARCHAR(30) NOT NULL,
        IssuedAt DATETIME NOT NULL DEFAULT SYSDATETIME(),
        IssuedByUserId INT NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Active',
        VoidedAt DATETIME NULL,
        VoidedByUserId INT NULL,
        VoidReason NVARCHAR(500) NULL,

        CONSTRAINT FK_Receipts_Payments FOREIGN KEY (PaymentId) REFERENCES Payments(PaymentId),
        CONSTRAINT FK_Receipts_Centers FOREIGN KEY (CenterId) REFERENCES Centers(CenterId),
        CONSTRAINT FK_Receipts_IssuedByUser FOREIGN KEY (IssuedByUserId) REFERENCES Users(UserId),
        CONSTRAINT FK_Receipts_VoidedByUser FOREIGN KEY (VoidedByUserId) REFERENCES Users(UserId)
    );

END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Receipts]') AND name = N'UX_Receipts_CenterId_ReceiptNumber')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Receipts_CenterId_ReceiptNumber ON Receipts(CenterId, ReceiptNumber);
END
GO

-- ========================================================
-- 5. TẠO BẢNG INVOICE CODE COUNTERS
-- ========================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InvoiceCodeCounters]') AND type in (N'U'))
BEGIN
    CREATE TABLE InvoiceCodeCounters (
        CenterId INT NOT NULL,
        YearMonth CHAR(6) NOT NULL, -- Định dạng: 'YYYYMM' (vd: '202606')
        LastNumber INT NOT NULL DEFAULT 0,
        
        PRIMARY KEY (CenterId, YearMonth),
        CONSTRAINT FK_InvoiceCodeCounters_Centers FOREIGN KEY (CenterId) REFERENCES Centers(CenterId)
    );
END
GO
