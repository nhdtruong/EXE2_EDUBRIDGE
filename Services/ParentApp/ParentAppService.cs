using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EduBridge.Services.Classes;
using EduBridge.Data;
using EduBridge.Models.DTOs.ParentApp;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Services.ParentApp;

public class ParentAppService : IParentAppService
{
    private readonly AppDbContext _context;

    public ParentAppService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ClassOperationResult<ParentDashboardDto>> GetDashboardAsync(int parentUserId, CancellationToken cancellationToken = default)
    {
        var parent = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == parentUserId && u.Status == "Active" && !u.IsDeleted, cancellationToken);

        if (parent == null)
            return ClassOperationResult<ParentDashboardDto>.Failure("Tài khoản phụ huynh không hợp lệ hoặc đã bị khóa.");

        var students = await _context.Students
            .Include(s => s.Enrollments)
            .Where(s => s.ParentUserId == parentUserId && s.Status == "Active" && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        var studentIds = students.Select(s => s.StudentId).ToList();

        var activeClassIds = students
            .SelectMany(s => s.Enrollments.Where(e => e.Status == "Đang học"))
            .Select(e => e.ClassId)
            .Distinct()
            .ToList();

        var unreadMessages = await _context.Messages
            .CountAsync(m => m.ReceiverUserId == parentUserId && !m.IsRead, cancellationToken);

        var unreadNotifications = await _context.Notifications
            .CountAsync(n => n.UserId == parentUserId && !n.IsRead, cancellationToken);

        var unpaidInvoices = await _context.Invoices
            .Where(i => studentIds.Contains(i.StudentId) && i.Status == "Unpaid")
            .SumAsync(i => i.Amount, cancellationToken);

        var today = DateOnly.FromDateTime(EduBridge.Helpers.TimeHelper.GetVietnamNow());
        var upcomingLessons = await _context.Lessons
            .Include(l => l.Class)
                .ThenInclude(c => c.RoomNavigation)
            .Where(l => activeClassIds.Contains(l.ClassId) && l.LessonDate >= today)
            .OrderBy(l => l.LessonDate)
            .ThenBy(l => l.StartTime)
            .Take(5)
            .ToListAsync(cancellationToken);

        var upcomingLessonsDto = new List<ParentUpcomingLessonDto>();
        foreach (var lesson in upcomingLessons)
        {
            var enrolledStudentsInClass = students
                .Where(s => s.Enrollments.Any(e => e.ClassId == lesson.ClassId && e.Status == "Đang học"))
                .ToList();

            foreach (var student in enrolledStudentsInClass)
            {
                upcomingLessonsDto.Add(new ParentUpcomingLessonDto
                {
                    LessonId = lesson.LessonId,
                    ClassId = lesson.ClassId,
                    ClassName = lesson.Class.ClassName,
                    LessonTitle = lesson.LessonTitle,
                    StudentId = student.StudentId,
                    StudentName = student.FullName,
                    LessonDate = lesson.LessonDate,
                    StartTime = lesson.StartTime,
                    EndTime = lesson.EndTime,
                    RoomName = lesson.Class.RoomNavigation?.RoomName ?? lesson.Class.Room
                });
            }
        }

        var dashboardDto = new ParentDashboardDto
        {
            ParentName = parent.FullName,
            TotalChildren = students.Count,
            TotalClasses = activeClassIds.Count,
            UnreadMessagesCount = unreadMessages,
            UnreadNotificationsCount = unreadNotifications,
            UnpaidInvoicesTotal = unpaidInvoices,
            Children = students.Select(s => new ParentChildOverviewDto
            {
                StudentId = s.StudentId,
                StudentCode = s.StudentCode,
                FullName = s.FullName,
                AvatarUrl = s.AvatarUrl,
                ActiveClassesCount = s.Enrollments.Count(e => e.Status == "Đang học")
            }).ToList(),
            UpcomingLessons = upcomingLessonsDto.OrderBy(l => l.LessonDate).ThenBy(l => l.StartTime).Take(5).ToList()
        };

        return ClassOperationResult<ParentDashboardDto>.Success(dashboardDto, "Thành công");
    }

    public async Task<ClassOperationResult<List<ParentChildOverviewDto>>> GetChildrenAsync(int parentUserId, CancellationToken cancellationToken = default)
    {
        var students = await _context.Students
            .AsNoTracking()
            .Include(s => s.Enrollments)
            .Where(s => s.ParentUserId == parentUserId && !s.IsDeleted)
            .Select(s => new ParentChildOverviewDto
            {
                StudentId = s.StudentId,
                StudentCode = s.StudentCode,
                FullName = s.FullName,
                AvatarUrl = s.AvatarUrl,
                ActiveClassesCount = s.Enrollments.Count(e => e.Status == "Đang học")
            })
            .ToListAsync(cancellationToken);

        return ClassOperationResult<List<ParentChildOverviewDto>>.Success(students, "Thành công");
    }

    public async Task<ClassOperationResult<ParentChildDetailDto>> GetChildDetailAsync(int parentUserId, int studentId, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students
            .AsNoTracking()
            .Include(s => s.Center)
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Class)
                    .ThenInclude(c => c.Course)
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Class)
                    .ThenInclude(c => c.Teacher)
                        .ThenInclude(t => t.User)
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.ParentUserId == parentUserId && !s.IsDeleted, cancellationToken);

        if (student == null)
            return ClassOperationResult<ParentChildDetailDto>.Failure("Không tìm thấy học sinh hoặc bạn không có quyền truy cập.");

        var dto = new ParentChildDetailDto
        {
            StudentId = student.StudentId,
            StudentCode = student.StudentCode,
            FullName = student.FullName,
            DateOfBirth = student.DateOfBirth,
            Gender = student.Gender,
            AvatarUrl = student.AvatarUrl,
            Status = student.Status,
            CenterName = student.Center.CenterName,
            Classes = student.Enrollments.Select(e => new ParentChildClassDto
            {
                ClassId = e.ClassId,
                ClassCode = e.Class.ClassCode,
                ClassName = e.Class.ClassName,
                CourseName = e.Class.Course?.CourseName ?? string.Empty,
                TeacherName = e.Class.Teacher?.User?.FullName ?? string.Empty,
                Status = e.Status,
                EnrolledAt = e.EnrollDate.ToDateTime(TimeOnly.MinValue)
            }).OrderByDescending(c => c.EnrolledAt).ToList()
        };

        return ClassOperationResult<ParentChildDetailDto>.Success(dto, "Thành công");
    }

    public async Task<ClassOperationResult<List<ParentClassOverviewDto>>> GetClassesAsync(int parentUserId, int studentId, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _context.Students
            .AsNoTracking()
            .AnyAsync(s => s.StudentId == studentId && s.ParentUserId == parentUserId && !s.IsDeleted, cancellationToken);

        if (!hasAccess)
            return ClassOperationResult<List<ParentClassOverviewDto>>.Failure("Không tìm thấy học sinh hoặc bạn không có quyền truy cập.");

        var classes = await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId && !e.Student.IsDeleted)
            .OrderByDescending(e => e.EnrollDate)
            .Select(e => new ParentClassOverviewDto
            {
                ClassId = e.ClassId,
                StudentId = e.StudentId,
                StudentName = e.Student.FullName,
                ClassCode = e.Class.ClassCode,
                ClassName = e.Class.ClassName,
                CourseName = e.Class.Course != null ? e.Class.Course.CourseName : string.Empty,
                TeacherName = e.Class.Teacher != null ? e.Class.Teacher.User.FullName : string.Empty,
                Status = e.Status
            })
            .ToListAsync(cancellationToken);

        return ClassOperationResult<List<ParentClassOverviewDto>>.Success(classes, "Thành công");
    }

    public async Task<ClassOperationResult<ParentClassDetailDto>> GetClassDetailAsync(int parentUserId, int studentId, int classId, CancellationToken cancellationToken = default)
    {
        var enrollment = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.Class)
                .ThenInclude(c => c.Course)
            .Include(e => e.Class)
                .ThenInclude(c => c.Teacher)
                    .ThenInclude(t => t.User)
            .FirstOrDefaultAsync(
                e => e.StudentId == studentId
                    && e.ClassId == classId
                    && e.Student.ParentUserId == parentUserId
                    && !e.Student.IsDeleted,
                cancellationToken);

        if (enrollment == null)
            return ClassOperationResult<ParentClassDetailDto>.Failure("Không tìm thấy lớp học hoặc bạn không có quyền truy cập.");

        var today = DateOnly.FromDateTime(EduBridge.Helpers.TimeHelper.GetVietnamNow());

        var upcomingLessons = await _context.Lessons
            .AsNoTracking()
            .Where(l => l.ClassId == classId && l.LessonDate >= today)
            .OrderBy(l => l.LessonDate)
            .ThenBy(l => l.StartTime)
            .Take(20)
            .Select(l => new ParentScheduleDto
            {
                LessonId = l.LessonId,
                ClassId = l.ClassId,
                ClassName = l.Class.ClassName,
                LessonTitle = l.LessonTitle,
                LessonDate = l.LessonDate,
                StartTime = l.StartTime,
                EndTime = l.EndTime,
                RoomName = l.Class.RoomNavigation != null ? l.Class.RoomNavigation.RoomName : l.Class.Room,
                TeacherName = l.Class.Teacher != null ? l.Class.Teacher.User.FullName : "Chưa phân công"
            })
            .ToListAsync(cancellationToken);

        var attendanceHistory = await _context.Attendances
            .AsNoTracking()
            .Where(a => a.StudentId == studentId && a.Lesson.ClassId == classId)
            .OrderByDescending(a => a.Lesson.LessonDate)
            .ThenByDescending(a => a.Lesson.StartTime)
            .Take(50)
            .Select(a => new ParentAttendanceDto
            {
                AttendanceId = a.AttendanceId,
                LessonId = a.LessonId,
                ClassName = a.Lesson.Class.ClassName,
                LessonTitle = a.Lesson.LessonTitle,
                LessonDate = a.Lesson.LessonDate,
                Status = a.Status,
                Note = a.Note
            })
            .ToListAsync(cancellationToken);

        var dto = new ParentClassDetailDto
        {
            ClassId = enrollment.ClassId,
            StudentId = enrollment.StudentId,
            StudentName = enrollment.Student.FullName,
            ClassCode = enrollment.Class.ClassCode,
            ClassName = enrollment.Class.ClassName,
            CourseName = enrollment.Class.Course != null ? enrollment.Class.Course.CourseName : string.Empty,
            TeacherName = enrollment.Class.Teacher != null ? enrollment.Class.Teacher.User.FullName : string.Empty,
            Status = enrollment.Status,
            UpcomingLessons = upcomingLessons,
            AttendanceHistory = attendanceHistory,
            AttendanceSummary = new ParentClassAttendanceSummaryDto
            {
                TotalLessons = attendanceHistory.Count,
                PresentCount = attendanceHistory.Count(a => a.Status == "Có mặt"),
                AbsentCount = attendanceHistory.Count(a => a.Status == "Vắng"),
                LateCount = attendanceHistory.Count(a => a.Status == "Muộn"),
                ExcusedCount = attendanceHistory.Count(a => a.Status == "Có phép")
            }
        };

        return ClassOperationResult<ParentClassDetailDto>.Success(dto, "Thành công");
    }

    public async Task<ClassOperationResult<List<ParentScheduleDto>>> GetScheduleAsync(int parentUserId, int? studentId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default)
    {
        var studentQuery = _context.Students.Where(s => s.ParentUserId == parentUserId && s.Status == "Active" && !s.IsDeleted);
        if (studentId.HasValue)
        {
            studentQuery = studentQuery.Where(s => s.StudentId == studentId.Value);
        }

        var students = await studentQuery.Select(s => new { s.StudentId, s.FullName }).ToListAsync(cancellationToken);
        if (!students.Any()) return ClassOperationResult<List<ParentScheduleDto>>.Failure("Không tìm thấy học sinh.");

        var studentIds = students.Select(s => s.StudentId).ToList();

        var enrollments = await _context.Enrollments
            .Where(e => studentIds.Contains(e.StudentId) && e.Status == "Đang học")
            .Select(e => new { e.StudentId, e.ClassId })
            .ToListAsync(cancellationToken);

        var classIds = enrollments.Select(e => e.ClassId).Distinct().ToList();

        var lessonsQuery = _context.Lessons
            .Include(l => l.Class)
                .ThenInclude(c => c.Teacher)
                    .ThenInclude(t => t.User)
            .Include(l => l.Class)
                .ThenInclude(c => c.RoomNavigation)
            .Where(l => classIds.Contains(l.ClassId));

        if (fromDate.HasValue) lessonsQuery = lessonsQuery.Where(l => l.LessonDate >= fromDate.Value);
        if (toDate.HasValue) lessonsQuery = lessonsQuery.Where(l => l.LessonDate <= toDate.Value);

        var lessons = await lessonsQuery
            .OrderBy(l => l.LessonDate)
            .ThenBy(l => l.StartTime)
            .ToListAsync(cancellationToken);

        var result = new List<ParentScheduleDto>();
        foreach (var lesson in lessons)
        {
            var enrolledStudents = enrollments.Where(e => e.ClassId == lesson.ClassId).Select(e => e.StudentId).ToList();
            foreach (var sid in enrolledStudents)
            {
                result.Add(new ParentScheduleDto
                {
                    LessonId = lesson.LessonId,
                    ClassId = lesson.ClassId,
                    ClassName = lesson.Class.ClassName,
                    LessonTitle = lesson.LessonTitle,
                    LessonDate = lesson.LessonDate,
                    StartTime = lesson.StartTime,
                    EndTime = lesson.EndTime,
                    RoomName = lesson.Class.RoomNavigation?.RoomName ?? lesson.Class.Room,
                    TeacherName = lesson.Class.Teacher?.User?.FullName ?? "Chưa phân công"
                });
            }
        }

        return ClassOperationResult<List<ParentScheduleDto>>.Success(result, "Thành công");
    }

    public async Task<ClassOperationResult<List<ParentAttendanceDto>>> GetAttendanceAsync(int parentUserId, int studentId, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _context.Students.AnyAsync(s => s.StudentId == studentId && s.ParentUserId == parentUserId && !s.IsDeleted, cancellationToken);
        if (!hasAccess) return ClassOperationResult<List<ParentAttendanceDto>>.Failure("Không có quyền truy cập học sinh này.");

        var attendances = await _context.Attendances
            .Include(a => a.Lesson)
                .ThenInclude(l => l.Class)
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.Lesson.LessonDate)
            .Take(50)
            .Select(a => new ParentAttendanceDto
            {
                AttendanceId = a.AttendanceId,
                LessonId = a.LessonId,
                ClassName = a.Lesson.Class.ClassName,
                LessonTitle = a.Lesson.LessonTitle,
                LessonDate = a.Lesson.LessonDate,
                Status = a.Status,
                Note = a.Note
            })
            .ToListAsync(cancellationToken);

        return ClassOperationResult<List<ParentAttendanceDto>>.Success(attendances, "Thành công");
    }

    public async Task<ClassOperationResult<List<ParentLessonDiaryDto>>> GetLessonDiaryAsync(int parentUserId, int studentId, CancellationToken cancellationToken = default)
    {
        var classIds = await _context.Enrollments
            .Where(e => e.StudentId == studentId && e.Student.ParentUserId == parentUserId && !e.Student.IsDeleted)
            .Select(e => e.ClassId)
            .ToListAsync(cancellationToken);

        if (classIds.Count == 0)
            return ClassOperationResult<List<ParentLessonDiaryDto>>.Failure("Không có quyền truy cập học sinh này.");

        var lessons = await _context.Lessons.AsNoTracking()
            .Where(l => classIds.Contains(l.ClassId) && l.LessonDate <= DateOnly.FromDateTime(EduBridge.Helpers.TimeHelper.GetVietnamNow()))
            .OrderByDescending(l => l.LessonDate).ThenByDescending(l => l.StartTime).Take(100)
            .Select(l => new ParentLessonDiaryDto
            {
                LessonId = l.LessonId,
                ClassName = l.Class.ClassName,
                TeacherName = l.Class.Teacher != null ? l.Class.Teacher.User.FullName : string.Empty,
                LessonTitle = l.LessonTitle,
                LessonContent = l.LessonContent,
                LessonDate = l.LessonDate,
                StartTime = l.StartTime,
                EndTime = l.EndTime
            }).ToListAsync(cancellationToken);

        return ClassOperationResult<List<ParentLessonDiaryDto>>.Success(lessons, "Thành công");
    }

    public async Task<ClassOperationResult<List<ParentGradeDto>>> GetGradesAsync(int parentUserId, int studentId, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _context.Students.AnyAsync(s => s.StudentId == studentId && s.ParentUserId == parentUserId && !s.IsDeleted, cancellationToken);
        if (!hasAccess) return ClassOperationResult<List<ParentGradeDto>>.Failure("Không có quyền truy cập học sinh này.");

        var grades = await _context.Grades
            .Include(g => g.Class)
            .Where(g => g.StudentId == studentId)
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new ParentGradeDto
            {
                GradeId = g.GradeId,
                ClassName = g.Class.ClassName,
                ExamName = g.GradeName,
                Score = g.Score,
                Comments = g.Comment,
                CreatedAt = g.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return ClassOperationResult<List<ParentGradeDto>>.Success(grades, "Thành công");
    }

    public async Task<ClassOperationResult<List<ParentHomeworkDto>>> GetHomeworksAsync(int parentUserId, int studentId, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _context.Students.AnyAsync(s => s.StudentId == studentId && s.ParentUserId == parentUserId && !s.IsDeleted, cancellationToken);
        if (!hasAccess) return ClassOperationResult<List<ParentHomeworkDto>>.Failure("Không có quyền truy cập học sinh này.");

        var enrollments = await _context.Enrollments.Where(e => e.StudentId == studentId && e.Status == "Đang học").Select(e => e.ClassId).ToListAsync(cancellationToken);
        var lessons = await _context.Lessons.Where(l => enrollments.Contains(l.ClassId)).Select(l => l.LessonId).ToListAsync(cancellationToken);

        var homeworks = await _context.Homeworks
            .Include(h => h.Lesson)
                .ThenInclude(l => l.Class)
            .Where(h => lessons.Contains(h.LessonId))
            .OrderByDescending(h => h.CreatedAt)
            .Take(30)
            .ToListAsync(cancellationToken);

        var submissions = await _context.HomeworkSubmissions
            .Where(s => s.StudentId == studentId && homeworks.Select(h => h.HomeworkId).Contains(s.HomeworkId))
            .ToListAsync(cancellationToken);

        var result = new List<ParentHomeworkDto>();
        foreach (var hw in homeworks)
        {
            var sub = submissions.FirstOrDefault(s => s.HomeworkId == hw.HomeworkId);
            result.Add(new ParentHomeworkDto
            {
                HomeworkId = hw.HomeworkId,
                ClassName = hw.Lesson.Class.ClassName,
                Title = hw.Title,
                DueDate = hw.DueDate ?? DateTime.MinValue,
                SubmissionStatus = sub == null ? "NotSubmitted" : (sub.Score.HasValue ? "Graded" : "Submitted"),
                Score = sub?.Score,
                TeacherFeedback = sub?.Feedback
            });
        }

        return ClassOperationResult<List<ParentHomeworkDto>>.Success(result, "Thành công");
    }

    public async Task<ClassOperationResult<List<ParentInvoiceDto>>> GetInvoicesAsync(int parentUserId, int? studentId, CancellationToken cancellationToken = default)
    {
        var studentQuery = _context.Students.Where(s => s.ParentUserId == parentUserId && !s.IsDeleted);
        if (studentId.HasValue) studentQuery = studentQuery.Where(s => s.StudentId == studentId.Value);

        var studentIds = await studentQuery.Select(s => s.StudentId).ToListAsync(cancellationToken);
        if (!studentIds.Any()) return ClassOperationResult<List<ParentInvoiceDto>>.Failure("Không tìm thấy học sinh.");

        var invoices = await _context.Invoices
            .Where(i => studentIds.Contains(i.StudentId))
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new ParentInvoiceDto
            {
                InvoiceId = i.InvoiceId,
                InvoiceCode = i.InvoiceCode,
                Title = i.Description ?? $"Hóa đơn {i.InvoiceCode}",
                Amount = i.Amount,
                AmountPaid = i.Payments.Sum(p => p.Amount),
                DueDate = i.DueDate ?? DateOnly.MinValue,
                Status = i.Status
            })
            .ToListAsync(cancellationToken);

        return ClassOperationResult<List<ParentInvoiceDto>>.Success(invoices, "Thành công");
    }

    public async Task<ClassOperationResult<List<ParentNotificationDto>>> GetNotificationsAsync(int parentUserId, CancellationToken cancellationToken = default)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == parentUserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new ParentNotificationDto
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Content = n.Content,
                Type = "SYSTEM",
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return ClassOperationResult<List<ParentNotificationDto>>.Success(notifications, "Thành công");
    }

    public async Task<ClassOperationResult<bool>> MarkNotificationAsReadAsync(int parentUserId, int notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == parentUserId, cancellationToken);

        if (notification == null) return ClassOperationResult<bool>.Failure("Không tìm thấy thông báo.");

        notification.IsRead = true;
        await _context.SaveChangesAsync(cancellationToken);
        return ClassOperationResult<bool>.Success(true, "Thành công");
    }

    public async Task<ClassOperationResult<List<ParentChatConversationDto>>> GetChatConversationsAsync(int parentUserId, CancellationToken cancellationToken = default)
    {
        var studentIds = await _context.Students
            .Where(s => s.ParentUserId == parentUserId && s.Status == "Active" && !s.IsDeleted)
            .Select(s => s.StudentId)
            .ToListAsync(cancellationToken);

        var classIds = await _context.Enrollments
            .Where(e => studentIds.Contains(e.StudentId) && e.Status == "Đang học")
            .Select(e => e.ClassId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var teacherUserIds = await _context.Classes
            .Where(c => classIds.Contains(c.ClassId) && c.Teacher != null)
            .Select(c => c.Teacher!.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var teachers = await _context.Users
            .Where(u => teacherUserIds.Contains(u.UserId))
            .Select(u => new { u.UserId, u.FullName, u.AvatarUrl })
            .ToListAsync(cancellationToken);

        var result = new List<ParentChatConversationDto>();

        foreach (var teacher in teachers)
        {
            var lastMessage = await _context.Messages
                .Where(m => (m.SenderUserId == parentUserId && m.ReceiverUserId == teacher.UserId) ||
                            (m.SenderUserId == teacher.UserId && m.ReceiverUserId == parentUserId))
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync(cancellationToken);

            var unreadCount = await _context.Messages
                .CountAsync(m => m.SenderUserId == teacher.UserId && m.ReceiverUserId == parentUserId && !m.IsRead, cancellationToken);

            result.Add(new ParentChatConversationDto
            {
                TeacherUserId = teacher.UserId,
                TeacherName = teacher.FullName,
                AvatarUrl = teacher.AvatarUrl,
                LastMessage = lastMessage?.Content ?? "Chưa có tin nhắn",
                LastMessageTime = lastMessage?.SentAt ?? DateTime.MinValue,
                UnreadCount = unreadCount
            });
        }

        return ClassOperationResult<List<ParentChatConversationDto>>.Success(result.OrderByDescending(r => r.LastMessageTime).ToList(), "Thành công");
    }

    public async Task<ClassOperationResult<List<ParentChatMessageDto>>> GetChatMessagesAsync(int parentUserId, int teacherUserId, CancellationToken cancellationToken = default)
    {
        if (!await CanParentChatWithTeacherAsync(parentUserId, teacherUserId, cancellationToken))
            return ClassOperationResult<List<ParentChatMessageDto>>.Failure("Không có quyền truy cập cuộc hội thoại này.");

        var messages = await _context.Messages
            .Where(m => (m.SenderUserId == parentUserId && m.ReceiverUserId == teacherUserId) ||
                        (m.SenderUserId == teacherUserId && m.ReceiverUserId == parentUserId))
            .OrderBy(m => m.SentAt)
            .Take(100)
            .Select(m => new ParentChatMessageDto
            {
                MessageId = m.MessageId,
                SenderId = m.SenderUserId,
                Content = m.Content,
                SentAt = m.SentAt,
                IsRead = m.IsRead
            })
            .ToListAsync(cancellationToken);

        return ClassOperationResult<List<ParentChatMessageDto>>.Success(messages, "Thành công");
    }

    public async Task<ClassOperationResult<bool>> MarkChatAsReadAsync(int parentUserId, int teacherUserId, CancellationToken cancellationToken = default)
    {
        if (!await CanParentChatWithTeacherAsync(parentUserId, teacherUserId, cancellationToken))
            return ClassOperationResult<bool>.Failure("Không có quyền truy cập cuộc hội thoại này.");

        await _context.Messages
            .Where(m => m.SenderUserId == teacherUserId && m.ReceiverUserId == parentUserId && !m.IsRead)
            .ExecuteUpdateAsync(update => update.SetProperty(m => m.IsRead, true), cancellationToken);
        return ClassOperationResult<bool>.Success(true, "Thành công");
    }

    private Task<bool> CanParentChatWithTeacherAsync(int parentUserId, int teacherUserId, CancellationToken cancellationToken) =>
        _context.Enrollments.AnyAsync(e =>
            e.Student.ParentUserId == parentUserId && !e.Student.IsDeleted && e.Status == "Đang học" &&
            e.Class.Teacher != null && e.Class.Teacher.UserId == teacherUserId,
            cancellationToken);
}
