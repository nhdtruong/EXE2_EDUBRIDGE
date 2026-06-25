USE EduBridgeDB;
GO

IF OBJECT_ID('dbo.LeaveRequests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeaveRequests (
        LeaveRequestId INT IDENTITY(1,1) PRIMARY KEY,
        StudentId INT NOT NULL,
        LessonId INT NOT NULL,
        ParentUserId INT NOT NULL,
        Reason NVARCHAR(1000) NOT NULL,
        Status VARCHAR(20) NOT NULL DEFAULT 'Pending',
        ReviewNote NVARCHAR(1000) NULL,
        ReviewedByUserId INT NULL,
        ReviewedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT FK_LeaveRequests_Student FOREIGN KEY(StudentId) REFERENCES Students(StudentId),
        CONSTRAINT FK_LeaveRequests_Lesson FOREIGN KEY(LessonId) REFERENCES Lessons(LessonId),
        CONSTRAINT FK_LeaveRequests_Parent FOREIGN KEY(ParentUserId) REFERENCES Users(UserId),
        CONSTRAINT FK_LeaveRequests_Reviewer FOREIGN KEY(ReviewedByUserId) REFERENCES Users(UserId),
        CONSTRAINT CK_LeaveRequests_Status CHECK(Status IN ('Pending','Approved','Rejected'))
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.LeaveRequests')
      AND name = N'IX_LeaveRequests_Student_CreatedAt'
)
BEGIN
    CREATE INDEX IX_LeaveRequests_Student_CreatedAt ON dbo.LeaveRequests(StudentId, CreatedAt DESC);
END
GO

IF OBJECT_ID('dbo.DevicePushTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DevicePushTokens (
        DevicePushTokenId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        ExpoPushToken NVARCHAR(255) NOT NULL UNIQUE,
        Platform VARCHAR(20) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT FK_DevicePushTokens_User FOREIGN KEY(UserId) REFERENCES Users(UserId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.DevicePushTokens')
      AND name = N'IX_DevicePushTokens_User_Active'
)
BEGIN
    CREATE INDEX IX_DevicePushTokens_User_Active ON dbo.DevicePushTokens(UserId, IsActive);
END
GO
