using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EduBridge.Data;
using EduBridge.Models;

namespace EduBridge.Pages.Teacher
{
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }

        public DateTime CurrentDate { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public int TotalClasses { get; set; }
        public int TotalStudents { get; set; }
        public int UngradedAssignments { get; set; }
        public int UnreadMessages { get; set; }

        public List<DashboardScheduleDto> TodaySchedules { get; set; } = new();
        public List<DashboardAssignmentDto> RecentAssignments { get; set; } = new();
        public List<DashboardMessageDto> RecentMessages { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            CurrentDate = DateTime.Now;

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return RedirectToPage("/Login");

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null)
                return RedirectToPage("/Login");

            TeacherName = teacher.User.FullName;

            var teacherClasses = await _context.Classes
                .Where(c => c.TeacherId == teacher.TeacherId && c.Status == "Active")
                .ToListAsync();

            TotalClasses = teacherClasses.Count;
            var classIds = teacherClasses.Select(c => c.ClassId).ToList();

            TotalStudents = await _context.Enrollments
                .Where(e => classIds.Contains(e.ClassId) && e.Status == "Đang học")
                .Select(e => e.StudentId)
                .Distinct()
                .CountAsync();

            // IsRead là bool không nullable - không cần ?? false
            UnreadMessages = await _context.Messages
                .CountAsync(m => m.ReceiverUserId == userId && !m.IsRead);

            // Lịch dạy hôm nay theo buổi học (Lesson) thực tế trong ngày hôm nay
            var todayDate = DateOnly.FromDateTime(DateTime.Now);
            var todayLessons = await _context.Lessons
                .Include(l => l.Class)
                .Where(l => classIds.Contains(l.ClassId) && l.LessonDate == todayDate)
                .OrderBy(l => l.StartTime)
                .Take(5)
                .ToListAsync();

            TodaySchedules = todayLessons.Select(l => new DashboardScheduleDto
            {
                ClassName = l.Class.ClassName,
                Topic = l.LessonTitle,
                TimeRange = (l.StartTime.HasValue && l.EndTime.HasValue)
                    ? $"{l.StartTime.Value.ToString("HH:mm")} - {l.EndTime.Value.ToString("HH:mm")}"
                    : "Chưa cấu hình giờ học",
                Room = l.Class.Room ?? "Không có phòng"
            }).ToList();

            // Lấy lessonIds để tìm homework
            var lessonIds = await _context.Lessons
                .Where(l => classIds.Contains(l.ClassId))
                .Select(l => l.LessonId)
                .ToListAsync();

            // Bài tập gần đây - lấy dữ liệu về trước rồi mới xử lý in-memory
            var recentHomeworks = await _context.Homeworks
                .Include(h => h.Lesson)
                .ThenInclude(l => l.Class)
                .Where(h => lessonIds.Contains(h.LessonId))
                .OrderByDescending(h => h.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Tính số học sinh enrolled cho từng lớp sau khi đã lấy về
            var enrollmentCounts = await _context.Enrollments
                .Where(e => classIds.Contains(e.ClassId) && e.Status == "Đang học")
                .GroupBy(e => e.ClassId)
                .Select(g => new { ClassId = g.Key, Count = g.Count() })
                .ToListAsync();
            var enrollmentDict = enrollmentCounts.ToDictionary(x => x.ClassId, x => x.Count);

            RecentAssignments = recentHomeworks.Select(h => new DashboardAssignmentDto
            {
                Title = h.Title,
                ClassName = h.Lesson.Class.ClassName,
                CreatedAt = h.CreatedAt,
                Submitted = 0, // Chưa có bảng Submission trong DB
                Total = enrollmentDict.GetValueOrDefault(h.Lesson.ClassId, 0)
            }).ToList();

            // Tin nhắn gần đây - lấy về in-memory rồi mới format
            var rawMessages = await _context.Messages
                .Include(m => m.SenderUser)
                .ThenInclude(u => u.Role)
                .Where(m => m.ReceiverUserId == userId)
                .OrderByDescending(m => m.SentAt)
                .Take(5)
                .ToListAsync();

            RecentMessages = rawMessages.Select(m => new DashboardMessageDto
            {
                SenderName = m.SenderUser.FullName,
                ParentInfo = m.SenderUser.Role?.RoleName == "PARENT" ? "Phụ huynh" : (m.SenderUser.Role?.RoleName ?? ""),
                Content = m.Content,
                TimeAgo = CalculateTimeAgo(m.SentAt),  // SentAt là DateTime không nullable
                Avatar = string.IsNullOrWhiteSpace(m.SenderUser.FullName)
                    ? "U"
                    : m.SenderUser.FullName.Substring(0, 1).ToUpper()
            }).ToList();

            return Page();
        }

        private static string CalculateTimeAgo(DateTime pastTime)
        {
            var span = DateTime.Now - pastTime;
            if (span.TotalDays >= 1) return $"{(int)span.TotalDays} ngày trước";
            if (span.TotalHours >= 1) return $"{(int)span.TotalHours} giờ trước";
            if (span.TotalMinutes >= 1) return $"{(int)span.TotalMinutes} phút trước";
            return "Vừa xong";
        }
    }

    public class DashboardScheduleDto
    {
        public string ClassName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string TimeRange { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
    }

    public class DashboardAssignmentDto
    {
        public string Title { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int Submitted { get; set; }
        public int Total { get; set; }
        public int PercentComplete => Total == 0 ? 0 : (int)((double)Submitted / Total * 100);
    }

    public class DashboardMessageDto
    {
        public string SenderName { get; set; } = string.Empty;
        public string ParentInfo { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
    }
}
