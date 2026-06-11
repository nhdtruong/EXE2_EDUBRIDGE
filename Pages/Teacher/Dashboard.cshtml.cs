using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using EduBridge.Services.Dashboard;

namespace EduBridge.Pages.Teacher
{
    public class DashboardModel : PageModel
    {
        private readonly IDashboardService _dashboardService;

        public DashboardModel(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
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

            var result = await _dashboardService.GetTeacherDashboardDataAsync(userId);
            if (!result.IsSuccess)
            {
                return RedirectToPage("/Login");
            }

            var data = result.Value!;

            TeacherName = data.TeacherName;
            TotalClasses = data.TotalClasses;
            TotalStudents = data.TotalStudents;
            UngradedAssignments = data.UngradedAssignmentsCount;
            UnreadMessages = data.UnreadMessagesCount;

            TodaySchedules = data.TodaySchedules.Select(s => new DashboardScheduleDto
            {
                ClassName = s.ClassName,
                Topic = s.Topic,
                TimeRange = s.TimeRange,
                Room = s.Room
            }).ToList();

            RecentAssignments = data.RecentAssignments.Select(a => new DashboardAssignmentDto
            {
                Title = a.Title,
                ClassName = a.ClassName,
                CreatedAt = a.CreatedAt ?? DateTime.Now,
                Submitted = a.SubmittedCount,
                Total = a.TotalStudents
            }).ToList();

            RecentMessages = data.RecentMessages.Select(m => new DashboardMessageDto
            {
                SenderName = m.SenderName,
                ParentInfo = m.SenderRole == "PARENT" ? "Phụ huynh" : m.SenderRole,
                Content = m.ShortContent,
                TimeAgo = CalculateTimeAgo(m.SentAt ?? DateTime.Now),
                Avatar = string.IsNullOrWhiteSpace(m.SenderName)
                    ? "U"
                    : m.SenderName.Substring(0, 1).ToUpper()
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
