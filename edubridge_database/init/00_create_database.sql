USE master;
GO

IF DB_ID(N'EduBridgeDB') IS NOT NULL
BEGIN
    ALTER DATABASE EduBridgeDB 
    SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

    DROP DATABASE EduBridgeDB;
END
GO

CREATE DATABASE EduBridgeDB;
GO

USE EduBridgeDB;
GO

/* =========================================================
   ROLES
========================================================= */

CREATE TABLE Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,

    RoleCode NVARCHAR(20) NOT NULL UNIQUE,
    RoleName NVARCHAR(50) NOT NULL
);
GO

/* =========================================================
   USERS
========================================================= */

CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,

    RoleId INT NOT NULL,

    FullName NVARCHAR(100) NOT NULL,

    Email NVARCHAR(150) NOT NULL UNIQUE,

    PasswordHash NVARCHAR(255) NOT NULL,

    PhoneNumber NVARCHAR(20) NULL,

    AvatarUrl NVARCHAR(500) NULL,

    EmailConfirmed BIT NOT NULL DEFAULT 1,

    Status NVARCHAR(20) NOT NULL DEFAULT N'Active',

    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    LastLoginAt DATETIME2 NULL,

    CONSTRAINT FK_Users_Roles
        FOREIGN KEY(RoleId)
        REFERENCES Roles(RoleId)
);
GO

/* =========================================================
   CENTERS
========================================================= */

CREATE TABLE Centers (
    CenterId INT IDENTITY(1,1) PRIMARY KEY,

    OwnerUserId INT NOT NULL,

    CenterName NVARCHAR(150) NOT NULL,

    Email NVARCHAR(150) NULL,

    PhoneNumber NVARCHAR(20) NULL,

    Address NVARCHAR(255) NULL,

    Status NVARCHAR(20) NOT NULL DEFAULT N'Active',

    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Centers_Users
        FOREIGN KEY(OwnerUserId)
        REFERENCES Users(UserId)
);
GO

/* =========================================================
   TEACHERS
========================================================= */

CREATE TABLE Teachers (
    TeacherId INT IDENTITY(1,1) PRIMARY KEY,

    UserId INT NOT NULL UNIQUE,

    CenterId INT NOT NULL,

    Specialization NVARCHAR(150) NULL,

    ExperienceYears INT NOT NULL DEFAULT 0,

    Status NVARCHAR(20) NOT NULL DEFAULT N'Active',

    CONSTRAINT FK_Teachers_Users
        FOREIGN KEY(UserId)
        REFERENCES Users(UserId),

    CONSTRAINT FK_Teachers_Centers
        FOREIGN KEY(CenterId)
        REFERENCES Centers(CenterId)
);
GO

/* =========================================================
   STUDENTS
========================================================= */

CREATE TABLE Students (
    StudentId INT IDENTITY(1,1) PRIMARY KEY,

    ParentUserId INT NOT NULL,

    CenterId INT NOT NULL,

    StudentCode NVARCHAR(30) NOT NULL UNIQUE,

    FullName NVARCHAR(100) NOT NULL,

    DateOfBirth DATE NULL,

    Gender NVARCHAR(10) NULL,

    Address NVARCHAR(255) NULL,

    Status NVARCHAR(20) NOT NULL DEFAULT N'Active',

    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Students_Users
        FOREIGN KEY(ParentUserId)
        REFERENCES Users(UserId),

    CONSTRAINT FK_Students_Centers
        FOREIGN KEY(CenterId)
        REFERENCES Centers(CenterId)
);
GO

/* =========================================================
   COURSES
========================================================= */

CREATE TABLE Courses (
    CourseId INT IDENTITY(1,1) PRIMARY KEY,

    CenterId INT NOT NULL,

    CourseName NVARCHAR(150) NOT NULL,

    Description NVARCHAR(500) NULL,

    DurationWeeks INT NOT NULL DEFAULT 12,

    TuitionFee DECIMAL(18,2) NULL,

    Status NVARCHAR(20) NOT NULL DEFAULT N'Active',

    CONSTRAINT FK_Courses_Centers
        FOREIGN KEY(CenterId)
        REFERENCES Centers(CenterId)
);
GO

/* =========================================================
   CLASSES
========================================================= */

CREATE TABLE Classes (
    ClassId INT IDENTITY(1,1) PRIMARY KEY,

    CenterId INT NOT NULL,

    CourseId INT NOT NULL,

    TeacherId INT NOT NULL,

    ClassCode NVARCHAR(30) NOT NULL UNIQUE,

    ClassName NVARCHAR(150) NOT NULL,

    Room NVARCHAR(50) NULL,

    ScheduleText NVARCHAR(255) NULL,

    StartDate DATE NOT NULL,

    EndDate DATE NOT NULL,

    MaxStudents INT NOT NULL DEFAULT 20,

    Status NVARCHAR(20) NOT NULL DEFAULT N'Active',

    CONSTRAINT FK_Classes_Centers
        FOREIGN KEY(CenterId)
        REFERENCES Centers(CenterId),

    CONSTRAINT FK_Classes_Courses
        FOREIGN KEY(CourseId)
        REFERENCES Courses(CourseId),

    CONSTRAINT FK_Classes_Teachers
        FOREIGN KEY(TeacherId)
        REFERENCES Teachers(TeacherId)
);
GO

/* =========================================================
   ENROLLMENTS
========================================================= */

CREATE TABLE Enrollments (
    EnrollmentId INT IDENTITY(1,1) PRIMARY KEY,

    StudentId INT NOT NULL,

    ClassId INT NOT NULL,

    EnrollDate DATE NOT NULL DEFAULT GETDATE(),

    Status NVARCHAR(20) NOT NULL DEFAULT N'Đang học',

    CONSTRAINT FK_Enrollments_Students
        FOREIGN KEY(StudentId)
        REFERENCES Students(StudentId),

    CONSTRAINT FK_Enrollments_Classes
        FOREIGN KEY(ClassId)
        REFERENCES Classes(ClassId),

    CONSTRAINT UQ_Enrollments
        UNIQUE(StudentId, ClassId)
);
GO

/* =========================================================
   LESSONS
========================================================= */

CREATE TABLE Lessons (
    LessonId INT IDENTITY(1,1) PRIMARY KEY,

    ClassId INT NOT NULL,

    LessonTitle NVARCHAR(200) NOT NULL,

    LessonDate DATE NOT NULL,

    LessonContent NVARCHAR(MAX) NULL,

    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Lessons_Classes
        FOREIGN KEY(ClassId)
        REFERENCES Classes(ClassId)
);
GO

/* =========================================================
   ATTENDANCE
========================================================= */

CREATE TABLE Attendance (
    AttendanceId INT IDENTITY(1,1) PRIMARY KEY,

    LessonId INT NOT NULL,

    StudentId INT NOT NULL,

    Status NVARCHAR(20) NOT NULL,

    Note NVARCHAR(255) NULL,

    RecordedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Attendance_Lessons
        FOREIGN KEY(LessonId)
        REFERENCES Lessons(LessonId),

    CONSTRAINT FK_Attendance_Students
        FOREIGN KEY(StudentId)
        REFERENCES Students(StudentId),

    CONSTRAINT UQ_Attendance
        UNIQUE(LessonId, StudentId)
);
GO

/* =========================================================
   HOMEWORK
========================================================= */

CREATE TABLE Homework (
    HomeworkId INT IDENTITY(1,1) PRIMARY KEY,

    LessonId INT NOT NULL,

    Title NVARCHAR(200) NOT NULL,

    Description NVARCHAR(MAX) NULL,

    DueDate DATETIME2 NULL,

    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Homework_Lessons
        FOREIGN KEY(LessonId)
        REFERENCES Lessons(LessonId)
);
GO

/* =========================================================
   GRADES
========================================================= */

CREATE TABLE Grades (
    GradeId INT IDENTITY(1,1) PRIMARY KEY,

    StudentId INT NOT NULL,

    ClassId INT NOT NULL,

    GradeName NVARCHAR(100) NOT NULL,

    Score DECIMAL(5,2) NOT NULL,

    Comment NVARCHAR(500) NULL,

    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Grades_Students
        FOREIGN KEY(StudentId)
        REFERENCES Students(StudentId),

    CONSTRAINT FK_Grades_Classes
        FOREIGN KEY(ClassId)
        REFERENCES Classes(ClassId)
);
GO

/* =========================================================
   MESSAGES
========================================================= */

CREATE TABLE Messages (
    MessageId INT IDENTITY(1,1) PRIMARY KEY,

    SenderUserId INT NOT NULL,

    ReceiverUserId INT NOT NULL,

    Content NVARCHAR(MAX) NOT NULL,

    SentAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    IsRead BIT NOT NULL DEFAULT 0,

    CONSTRAINT FK_Messages_Sender
        FOREIGN KEY(SenderUserId)
        REFERENCES Users(UserId),

    CONSTRAINT FK_Messages_Receiver
        FOREIGN KEY(ReceiverUserId)
        REFERENCES Users(UserId)
);
GO

/* =========================================================
   NOTIFICATIONS
========================================================= */

CREATE TABLE Notifications (
    NotificationId INT IDENTITY(1,1) PRIMARY KEY,

    UserId INT NOT NULL,

    Title NVARCHAR(200) NOT NULL,

    Content NVARCHAR(MAX) NOT NULL,

    IsRead BIT NOT NULL DEFAULT 0,

    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Notifications_Users
        FOREIGN KEY(UserId)
        REFERENCES Users(UserId)
);
GO

/* =========================================================
   INDEXES
========================================================= */

CREATE INDEX IX_Users_Email
ON Users(Email);

CREATE INDEX IX_Users_RoleId_Status
ON Users(RoleId, Status);

CREATE INDEX IX_Students_ParentUserId
ON Students(ParentUserId);

CREATE INDEX IX_Students_CenterId
ON Students(CenterId);

CREATE INDEX IX_Students_StudentCode
ON Students(StudentCode);

CREATE INDEX IX_Teachers_UserId
ON Teachers(UserId);

CREATE INDEX IX_Teachers_CenterId
ON Teachers(CenterId);

CREATE INDEX IX_Classes_CourseId
ON Classes(CourseId);

CREATE INDEX IX_Classes_TeacherId
ON Classes(TeacherId);

CREATE INDEX IX_Classes_CenterId
ON Classes(CenterId);

CREATE INDEX IX_Classes_ClassCode
ON Classes(ClassCode);

CREATE INDEX IX_Enrollments_StudentId
ON Enrollments(StudentId);

CREATE INDEX IX_Enrollments_ClassId
ON Enrollments(ClassId);

CREATE INDEX IX_Lessons_ClassId_LessonDate
ON Lessons(ClassId, LessonDate);

CREATE INDEX IX_Attendance_LessonId
ON Attendance(LessonId);

CREATE INDEX IX_Attendance_StudentId
ON Attendance(StudentId);

CREATE INDEX IX_Messages_SenderReceiver
ON Messages(SenderUserId, ReceiverUserId);

CREATE INDEX IX_Messages_Receiver_IsRead
ON Messages(ReceiverUserId, IsRead);

CREATE INDEX IX_Notifications_User_Read
ON Notifications(UserId, IsRead);
GO

/* =========================================================
   VIEWS
========================================================= */

CREATE VIEW vw_StudentOverview AS
SELECT
    s.StudentId,
    s.StudentCode,
    s.FullName AS StudentName,
    u.FullName AS ParentName,
    u.PhoneNumber AS ParentPhone,
    c.CenterName,
    s.Status
FROM Students s
JOIN Users u
ON s.ParentUserId = u.UserId
JOIN Centers c
ON s.CenterId = c.CenterId;
GO

CREATE VIEW vw_ClassOverview AS
SELECT
    cl.ClassId,
    cl.ClassCode,
    cl.ClassName,
    co.CourseName,
    center.CenterName,
    u.FullName AS TeacherName,
    cl.ScheduleText,
    cl.Room,
    cl.StartDate,
    cl.EndDate,
    cl.Status,
    COUNT(e.EnrollmentId) AS TotalStudents
FROM Classes cl

JOIN Courses co
ON cl.CourseId = co.CourseId

JOIN Centers center
ON cl.CenterId = center.CenterId

JOIN Teachers t
ON cl.TeacherId = t.TeacherId

JOIN Users u
ON t.UserId = u.UserId

LEFT JOIN Enrollments e
ON cl.ClassId = e.ClassId

GROUP BY
    cl.ClassId,
    cl.ClassCode,
    cl.ClassName,
    co.CourseName,
    center.CenterName,
    u.FullName,
    cl.ScheduleText,
    cl.Room,
    cl.StartDate,
    cl.EndDate,
    cl.Status;
GO

CREATE VIEW vw_AttendanceSummary AS
SELECT
    l.LessonId,
    l.LessonTitle,
    l.LessonDate,
    cl.ClassName,

    COUNT(a.AttendanceId) AS TotalRecords,

    SUM(
        CASE
            WHEN a.Status = N'Có mặt'
            THEN 1
            ELSE 0
        END
    ) AS PresentCount,

    SUM(
        CASE
            WHEN a.Status = N'Vắng'
            THEN 1
            ELSE 0
        END
    ) AS AbsentCount

FROM Lessons l

JOIN Classes cl
ON l.ClassId = cl.ClassId

LEFT JOIN Attendance a
ON l.LessonId = a.LessonId

GROUP BY
    l.LessonId,
    l.LessonTitle,
    l.LessonDate,
    cl.ClassName;
GO

/* =========================================================
   SEED DATA
========================================================= */

INSERT INTO Roles(RoleCode, RoleName)
VALUES
('OWNER', N'Chủ trung tâm'),
('TEACHER', N'Giáo viên'),
('PARENT', N'Phụ huynh');
GO

/* 
Password demo: 123456
BCrypt hash demo
*/

INSERT INTO Users
(
    RoleId,
    FullName,
    Email,
    PasswordHash,
    PhoneNumber
)
VALUES
(
    1,
    N'Nguyễn Thu Hà',
    'owner@edubridge.com',
    '$2a$11$QeDkI0UqH8Nn7vFpGcxmE.cXjjJxZmwGI6pCJmp1zWE0wuOXsAcDa',
    '0900000001'
),
(
    2,
    N'Phạm Quốc Huy',
    'teacher@edubridge.com',
    '$2a$11$QeDkI0UqH8Nn7vFpGcxmE.cXjjJxZmwGI6pCJmp1zWE0wuOXsAcDa',
    '0900000002'
),
(
    3,
    N'Phạm Thị Lan',
    'parent@edubridge.com',
    '$2a$11$QeDkI0UqH8Nn7vFpGcxmE.cXjjJxZmwGI6pCJmp1zWE0wuOXsAcDa',
    '0911000001'
);
GO

INSERT INTO Centers
(
    OwnerUserId,
    CenterName,
    Email,
    PhoneNumber,
    Address
)
VALUES
(
    1,
    N'Trung tâm Anh ngữ Sao Mai',
    'saomai@gmail.com',
    '02438889999',
    N'Cầu Giấy, Hà Nội'
);
GO

INSERT INTO Teachers
(
    UserId,
    CenterId,
    Specialization,
    ExperienceYears
)
VALUES
(
    2,
    1,
    N'IELTS Foundation',
    5
);
GO

INSERT INTO Students
(
    ParentUserId,
    CenterId,
    StudentCode,
    FullName,
    DateOfBirth,
    Gender
)
VALUES
(
    3,
    1,
    'STD001',
    N'Nguyễn Bảo An',
    '2015-04-12',
    N'Nữ'
),
(
    3,
    1,
    'STD002',
    N'Nguyễn Gia Hân',
    '2014-09-10',
    N'Nữ'
);
GO

INSERT INTO Courses
(
    CenterId,
    CourseName,
    Description,
    DurationWeeks,
    TuitionFee
)
VALUES
(
    1,
    N'Kids English A1',
    N'Khóa tiếng Anh thiếu nhi nền tảng',
    12,
    3500000
);
GO

INSERT INTO Classes
(
    CenterId,
    CourseId,
    TeacherId,
    ClassCode,
    ClassName,
    Room,
    ScheduleText,
    StartDate,
    EndDate
)
VALUES
(
    1,
    1,
    1,
    'CLS001',
    N'Kids English A1 - Lớp 01',
    N'Phòng 101',
    N'Thứ 2, Thứ 4 - 18:00',
    '2026-05-01',
    '2026-08-01'
);
GO

INSERT INTO Enrollments
(
    StudentId,
    ClassId
)
VALUES
(1, 1),
(2, 1);
GO

INSERT INTO Lessons
(
    ClassId,
    LessonTitle,
    LessonDate,
    LessonContent
)
VALUES
(
    1,
    N'Bài 1: Chào hỏi và giới thiệu bản thân',
    '2026-05-04',
    N'Học mẫu câu chào hỏi cơ bản'
);
GO

INSERT INTO Attendance
(
    LessonId,
    StudentId,
    Status
)
VALUES
(1, 1, N'Có mặt'),
(1, 2, N'Đi muộn');
GO

INSERT INTO Homework
(
    LessonId,
    Title,
    Description,
    DueDate
)
VALUES
(
    1,
    N'Luyện tập chào hỏi',
    N'Ghi âm đoạn hội thoại chào hỏi',
    '2026-05-20'
);
GO

INSERT INTO Grades
(
    StudentId,
    ClassId,
    GradeName,
    Score,
    Comment
)
VALUES
(
    1,
    1,
    N'Kiểm tra từ vựng',
    9,
    N'Phát âm tốt'
),
(
    2,
    1,
    N'Kiểm tra từ vựng',
    7.5,
    N'Cần luyện thêm'
);
GO

INSERT INTO Messages
(
    SenderUserId,
    ReceiverUserId,
    Content
)
VALUES
(
    2,
    3,
    N'Bảo An hôm nay học rất tốt.'
);
GO

INSERT INTO Notifications
(
    UserId,
    Title,
    Content
)
VALUES
(
    3,
    N'Thông báo mới',
    N'Bạn có thông báo mới từ giáo viên.'
);
GO

UPDATE Users
SET PasswordHash = '$2a$11$4AS8zQyRXz.pz14rBox1TekJ5O2z4uSzRVPaPbtogeEi2bZ3xnCAC'
WHERE Email IN (
    'owner@edubridge.com',
    'teacher@edubridge.com',
    'parent@edubridge.com'
);