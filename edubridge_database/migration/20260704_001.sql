USE EduBridgeDB;
GO

IF COL_LENGTH('Branches', 'LogoUrl') IS NULL
BEGIN
    ALTER TABLE Branches
    ADD LogoUrl NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH('Branches', 'ImageUrl') IS NULL
BEGIN
    ALTER TABLE Branches
    ADD ImageUrl NVARCHAR(MAX) NULL;
END
GO
