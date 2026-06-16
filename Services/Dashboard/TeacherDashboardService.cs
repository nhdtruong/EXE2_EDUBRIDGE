using EduBridge.Contracts.Dashboard;
using EduBridge.Data;
using EduBridge.Helpers;
using EduBridge.Services.Classes;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Services.Dashboard;

public sealed class TeacherDashboardService : ITeacherDashboardService
{
    private readonly AppDbContext _context;

    public TeacherDashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ClassOperationResult<TeacherDashboardSummaryResponse>> GetDashboardSummaryAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var teacher = await _context.Teachers
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken);

        if (teacher == null)
        {
            return ClassOperationResult<TeacherDashboardSummaryResponse>.Failure("Không tìm thấy giáo viên.", new Dictionary<string, string[]>());
        }

        var teacherName = teacher.User.FullName;

        var teacherClasses = await _context.Classes
            .Where(c => c.TeacherId == teacher.TeacherId && c.Status == "Active")
            .ToListAsync(cancellationToken);

        var totalClasses = teacherClasses.Count;
        var classIds = teacherClasses.Select(c => c.ClassId).ToList();

        var totalStudents = await _context.Enrollments
            .Where(e => classIds.Contains(e.ClassId) && e.Status == "Đang học")
            .Select(e => e.StudentId)
            .Distinct()
            .CountAsync(cancellationToken);

        var unreadMessages = await _context.Messages
            .CountAsync(m => m.ReceiverUserId == userId && !m.IsRead, cancellationToken);

        var todayDate = DateOnly.FromDateTime(TimeHelper.GetVietnamNow());
        var todayLessons = await _context.Lessons
            .Include(l => l.Class)
            .Where(l => classIds.Contains(l.ClassId) && l.LessonDate == todayDate)
            .OrderBy(l => l.StartTime)
            .Take(5)
            .ToListAsync(cancellationToken);

        var todaySchedules = todayLessons.Select(l => new TeacherDashboardScheduleDto(
            ClassName: l.Class.ClassName,
            Topic: l.LessonTitle,
            TimeRange: (l.StartTime.HasValue && l.EndTime.HasValue)
                ? $"{l.StartTime.Value.ToString("HH\\:mm")} - {l.EndTime.Value.ToString("HH\\:mm")}"
                : "Chưa cấu hình giờ học",
            Room: l.Class.Room ?? "Không có phòng"
        )).ToList();

        var lessonIds = await _context.Lessons
            .Where(l => classIds.Contains(l.ClassId))
            .Select(l => l.LessonId)
            .ToListAsync(cancellationToken);

        var recentHomeworks = await _context.Homeworks
            .Include(h => h.Lesson)
            .ThenInclude(l => l.Class)
            .Include(h => h.HomeworkSubmissions)
            .Where(h => lessonIds.Contains(h.LessonId))
            .OrderByDescending(h => h.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var enrollmentCounts = await _context.Enrollments
            .Where(e => classIds.Contains(e.ClassId) && e.Status == "Đang học")
            .GroupBy(e => e.ClassId)
            .Select(g => new { ClassId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var enrollmentDict = enrollmentCounts.ToDictionary(x => x.ClassId, x => x.Count);

        var recentAssignments = recentHomeworks.Select(h => {
            var submitted = h.HomeworkSubmissions.Count;
            var total = enrollmentDict.GetValueOrDefault(h.Lesson.ClassId, 0);
            var percent = total == 0 ? 0 : (int)((double)submitted / total * 100);
            return new TeacherDashboardAssignmentDto(
                Title: h.Title,
                ClassName: h.Lesson.Class.ClassName,
                CreatedAt: h.CreatedAt,
                Submitted: submitted,
                Total: total,
                PercentComplete: percent
            );
        }).ToList();

        // Tính số bài tập nộp chưa được chấm điểm (Status == "Submitted")
        var ungradedAssignments = await _context.HomeworkSubmissions
            .CountAsync(s => lessonIds.Contains(s.Homework.LessonId) && s.Status == "Submitted", cancellationToken);

        var rawMessages = await _context.Messages
            .Include(m => m.SenderUser)
            .ThenInclude(u => u.Role)
            .Where(m => m.ReceiverUserId == userId)
            .OrderByDescending(m => m.SentAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentMessages = rawMessages.Select(m => new TeacherDashboardMessageDto(
            SenderName: m.SenderUser.FullName,
            ParentInfo: m.SenderUser.Role?.RoleName == "PARENT" ? "Phụ huynh" : (m.SenderUser.Role?.RoleName ?? ""),
            Content: m.Content,
            TimeAgo: CalculateTimeAgo(m.SentAt),
            Avatar: string.IsNullOrWhiteSpace(m.SenderUser.FullName)
                ? "U"
                : m.SenderUser.FullName.Substring(0, 1).ToUpper()
        )).ToList();

        var response = new TeacherDashboardSummaryResponse(
            teacherName,
            totalClasses,
            totalStudents,
            ungradedAssignments,
            unreadMessages,
            todaySchedules,
            recentAssignments,
            recentMessages
        );

        return ClassOperationResult<TeacherDashboardSummaryResponse>.Success(response, "Success");
    }

    private static string CalculateTimeAgo(DateTime pastTime)
    {
        var span = TimeHelper.GetVietnamNow() - pastTime;
        if (span.TotalDays >= 1) return $"{(int)span.TotalDays} ngày trước";
        if (span.TotalHours >= 1) return $"{(int)span.TotalHours} giờ trước";
        if (span.TotalMinutes >= 1) return $"{(int)span.TotalMinutes} phút trước";
        return "Vừa xong";
    }
}
