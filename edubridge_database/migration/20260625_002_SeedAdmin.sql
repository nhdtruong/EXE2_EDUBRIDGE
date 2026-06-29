USE EduBridgeDB;
GO

DECLARE @AdminRoleId INT;
SELECT @AdminRoleId = RoleId FROM Roles WHERE RoleCode = 'SYSTEM_ADMIN';

IF @AdminRoleId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'admin@edubridge.com')
    BEGIN
        INSERT INTO Users (
            RoleId, 
            FullName, 
            Email, 
            PasswordHash, 
            EmailConfirmed, 
            Status, 
            CreatedAt,
            IsDeleted
        )
        VALUES (
            @AdminRoleId, 
            N'System Administrator', 
            'admin@edubridge.com', 
            '$2a$11$4AS8zQyRXz.pz14rBox1TekJ5O2z4uSzRVPaPbtogeEi2bZ3xnCAC', 
            1, 
            N'Active', 
            SYSDATETIME(),
            0
        );
    END
END
GO
