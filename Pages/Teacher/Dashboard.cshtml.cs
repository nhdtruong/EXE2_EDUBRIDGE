using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using EduBridge.Contracts.Dashboard;
using EduBridge.Services.Dashboard;

namespace EduBridge.Pages.Teacher
{
    public class DashboardModel : PageModel
    {
        private readonly ITeacherDashboardService _dashboardService;

        public DashboardModel(ITeacherDashboardService dashboardService)
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

            var result = await _dashboardService.GetDashboardSummaryAsync(userId, cancellationToken);
            
            if (!result.IsSuccess || result.Value == null)
            {
                return RedirectToPage("/Login");
            }

            var summary = result.Value;
            TeacherName = summary.TeacherName;
            TotalClasses = summary.TotalClasses;
            TotalStudents = summary.TotalStudents;
            UngradedAssignments = summary.UngradedAssignments;
            UnreadMessages = summary.UnreadMessages;
            TodaySchedules = summary.TodaySchedules;
            RecentAssignments = summary.RecentAssignments;
            RecentMessages = summary.RecentMessages;

            return Page();
        }
    }
}
