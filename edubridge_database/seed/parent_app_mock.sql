USE EduBridgeDB;
GO

SET NOCOUNT ON;

DECLARE @ParentId INT = (SELECT UserId FROM Users WHERE Email = 'parent@edubridge.com');
DECLARE @TeacherId INT = (SELECT UserId FROM Users WHERE Email = 'teacher@edubridge.com');
DECLARE @OwnerId INT = (SELECT UserId FROM Users WHERE Email = 'owner@edubridge.com');
DECLARE @CenterId INT = (SELECT TOP 1 CenterId FROM Centers ORDER BY CenterId);
DECLARE @ClassId INT = (SELECT TOP 1 ClassId FROM Classes WHERE CenterId = @CenterId ORDER BY ClassId);
DECLARE @Today DATE = CAST(GETDATE() AS DATE);

IF @ParentId IS NULL OR @TeacherId IS NULL OR @OwnerId IS NULL OR @CenterId IS NULL OR @ClassId IS NULL
    THROW 51000, 'Base seed data is missing.', 1;

IF NOT EXISTS (SELECT 1 FROM CenterUsers WHERE CenterId=@CenterId AND UserId=@ParentId)
    INSERT CenterUsers(CenterId, UserId, UserType, Status) VALUES(@CenterId, @ParentId, 'PARENT', 'Active');
IF NOT EXISTS (SELECT 1 FROM CenterUsers WHERE CenterId=@CenterId AND UserId=@TeacherId)
    INSERT CenterUsers(CenterId, UserId, UserType, Status) VALUES(@CenterId, @TeacherId, 'TEACHER', 'Active');

DECLARE @Student1 INT = (SELECT StudentId FROM Students WHERE StudentCode='STD001');
DECLARE @Student2 INT = (SELECT StudentId FROM Students WHERE StudentCode='STD002');

DECLARE @Days TABLE(OffsetDay INT, Title NVARCHAR(200), Content NVARCHAR(MAX), StartTime TIME, EndTime TIME);
INSERT @Days VALUES
(-7, N'Ôn tập từ vựng tuần trước', N'Ôn tập chủ đề gia đình và trường học.', '18:00', '19:30'),
(-2, N'Luyện nghe và phản xạ', N'Luyện nghe đoạn hội thoại ngắn và trả lời câu hỏi.', '18:00', '19:30'),
(0, N'Giao tiếp theo tình huống', N'Thực hành hội thoại tại lớp và sửa phát âm.', '18:00', '19:30'),
(2, N'Đọc hiểu truyện ngắn', N'Đọc hiểu, tìm ý chính và từ khóa.', '18:00', '19:30'),
(5, N'Kiểm tra cuối chủ đề', N'Đánh giá từ vựng, nghe và giao tiếp.', '18:00', '19:30');

INSERT Lessons(ClassId, LessonTitle, LessonDate, LessonContent, StartTime, EndTime, Status)
SELECT @ClassId, d.Title, DATEADD(DAY,d.OffsetDay,@Today), d.Content, d.StartTime, d.EndTime, 'Scheduled'
FROM @Days d
WHERE NOT EXISTS (SELECT 1 FROM Lessons l WHERE l.ClassId=@ClassId AND l.LessonDate=DATEADD(DAY,d.OffsetDay,@Today) AND l.LessonTitle=d.Title);

DECLARE @TodayLesson INT = (SELECT TOP 1 LessonId FROM Lessons WHERE ClassId=@ClassId AND LessonDate=@Today ORDER BY LessonId DESC);
DECLARE @PastLesson INT = (SELECT TOP 1 LessonId FROM Lessons WHERE ClassId=@ClassId AND LessonDate=DATEADD(DAY,-2,@Today) ORDER BY LessonId DESC);

IF NOT EXISTS (SELECT 1 FROM Attendance WHERE LessonId=@TodayLesson AND StudentId=@Student1)
    INSERT Attendance(LessonId,StudentId,Status,Note) VALUES(@TodayLesson,@Student1,N'Vắng',N'Chưa ghi nhận có mặt');
IF NOT EXISTS (SELECT 1 FROM Attendance WHERE LessonId=@TodayLesson AND StudentId=@Student2)
    INSERT Attendance(LessonId,StudentId,Status) VALUES(@TodayLesson,@Student2,N'Có mặt');
IF NOT EXISTS (SELECT 1 FROM Attendance WHERE LessonId=@PastLesson AND StudentId=@Student1)
    INSERT Attendance(LessonId,StudentId,Status) VALUES(@PastLesson,@Student1,N'Muộn');

IF NOT EXISTS (SELECT 1 FROM Homework WHERE LessonId=@TodayLesson AND Title=N'Bài tập giao tiếp tại nhà')
    INSERT Homework(LessonId,Title,Description,DueDate)
    VALUES(@TodayLesson,N'Bài tập giao tiếp tại nhà',N'Ghi âm đoạn hội thoại 2 phút.',DATEADD(DAY,2,GETDATE()));

DECLARE @HomeworkId INT = (SELECT TOP 1 HomeworkId FROM Homework WHERE LessonId=@TodayLesson ORDER BY HomeworkId DESC);
IF NOT EXISTS (SELECT 1 FROM HomeworkSubmissions WHERE HomeworkId=@HomeworkId AND StudentId=@Student2)
    INSERT HomeworkSubmissions(HomeworkId,StudentId,SubmissionContent,SubmittedAt,Score,Feedback,Status)
    VALUES(@HomeworkId,@Student2,N'Đã nộp file ghi âm.',GETDATE(),8.5,N'Phát âm tốt, cần nói chậm hơn.','Graded');

IF NOT EXISTS (SELECT 1 FROM Grades WHERE StudentId=@Student1 AND GradeName=N'Kiểm tra giao tiếp tháng này')
    INSERT Grades(StudentId,ClassId,GradeName,Score,Comment)
    VALUES(@Student1,@ClassId,N'Kiểm tra giao tiếp tháng này',8.5,N'Tiến bộ tốt, chủ động giao tiếp.');

IF NOT EXISTS (SELECT 1 FROM Invoices WHERE StudentId=@Student1 AND InvoiceCode='MOBILE-MOCK-001')
    INSERT Invoices(StudentId,ClassId,Amount,DiscountAmount,DueDate,Status,CenterId,InvoiceCode,Description,CreatedByUserId)
    VALUES(@Student1,@ClassId,3500000,0,DATEADD(DAY,5,@Today),'Unpaid',@CenterId,'MOBILE-MOCK-001',N'Học phí kỳ tiếp theo',@OwnerId);

IF NOT EXISTS (SELECT 1 FROM Notifications WHERE UserId=@ParentId AND Title=N'Lịch học và bài tập mới')
    INSERT Notifications(UserId,Title,Content,IsRead,CreatedAt)
    VALUES(@ParentId,N'Lịch học và bài tập mới',N'Trung tâm đã cập nhật lịch học và bài tập trong tuần.',0,GETDATE());

IF NOT EXISTS (SELECT 1 FROM Messages WHERE SenderUserId=@TeacherId AND ReceiverUserId=@ParentId AND Content=N'Chào phụ huynh, đây là tin nhắn test Parent App.')
    INSERT Messages(SenderUserId,ReceiverUserId,Content,SentAt,IsRead)
    VALUES(@TeacherId,@ParentId,N'Chào phụ huynh, đây là tin nhắn test Parent App.',GETDATE(),0);

IF NOT EXISTS (SELECT 1 FROM LeaveRequests WHERE StudentId=@Student1 AND LessonId=(SELECT TOP 1 LessonId FROM Lessons WHERE ClassId=@ClassId AND LessonDate=DATEADD(DAY,5,@Today)))
    INSERT LeaveRequests(StudentId,LessonId,ParentUserId,Reason,Status,CreatedAt)
    SELECT @Student1, TOP_LESSON.LessonId, @ParentId, N'Gia đình có lịch cá nhân.', 'Pending', GETDATE()
    FROM (SELECT TOP 1 LessonId FROM Lessons WHERE ClassId=@ClassId AND LessonDate=DATEADD(DAY,5,@Today)) TOP_LESSON;

PRINT 'Parent App mock seed completed.';
GO
