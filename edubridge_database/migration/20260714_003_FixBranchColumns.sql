USE EduBridgeDB;
GO

IF COL_LENGTH('Branches', 'Description') IS NULL
BEGIN
    ALTER TABLE Branches
    ADD Description NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH('Branches', 'HeadUserId') IS NULL
BEGIN
    ALTER TABLE Branches
    ADD HeadUserId INT NULL;
END
GO

