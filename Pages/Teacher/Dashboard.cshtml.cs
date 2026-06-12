using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
<<<<<<< HEAD
using EduBridge.Contracts.Dashboard;
=======
>>>>>>> origin/main
using EduBridge.Services.Dashboard;

namespace EduBridge.Pages.Teacher
{
    public class DashboardModel : PageModel
    {
<<<<<<< HEAD
        private readonly ITeacherDashboardService _dashboardService;

        public DashboardModel(ITeacherDashboardService dashboardService)
=======
        private readonly IDashboardService _dashboardService;

        public DashboardModel(IDashboardService dashboardService)
>>>>>>> origin/main
        {
            _dashboardService = dashboardService;
        }

        public DateTime CurrentDate { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public int TotalClasses { get; set; }
        public int TotalStudents { get; set; }
        public int UngradedAssignments { get; set; }
        public int UnreadMessages { get; set; }

        public IReadOnlyList<TeacherDashboardScheduleDto> TodaySchedules { get; set; } = new List<TeacherDashboardScheduleDto>();
        public IReadOnlyList<TeacherDashboardAssignmentDto> RecentAssignments { get; set; } = new List<TeacherDashboardAssignmentDto>();
        public IReadOnlyList<TeacherDashboardMessageDto> RecentMessages { get; set; } = new List<TeacherDashboardMessageDto>();

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            CurrentDate = EduBridge.Helpers.TimeHelper.GetVietnamNow();

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return RedirectToPage("/Login");

            var result = await _dashboardService.GetTeacherDashboardDataAsync(userId);
            if (!result.IsSuccess || result.Value == null)
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
    }
}
