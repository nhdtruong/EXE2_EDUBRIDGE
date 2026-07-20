USE EduBridgeDB
GO

-- Add StaffCode to Users table
IF COL_LENGTH(N'dbo.Users', N'StaffCode') IS NULL
BEGIN
    ALTER TABLE Users
    ADD StaffCode NVARCHAR(50) NULL;
END;
GO
