using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
<<<<<<< HEAD
=======

>>>>>>> e5417bb24ce6b520875746ee3d72982295df8d14
using EduBridge.Contracts.Dashboard;
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
>>>>>>> e5417bb24ce6b520875746ee3d72982295df8d14
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

<<<<<<< HEAD
            var result = await _dashboardService.GetDashboardSummaryAsync(userId, cancellationToken);
            
=======
            var result = await _dashboardService.GetTeacherDashboardDataAsync(userId);
>>>>>>> e5417bb24ce6b520875746ee3d72982295df8d14
            if (!result.IsSuccess || result.Value == null)
            {
                return RedirectToPage("/Login");
            }

<<<<<<< HEAD
            var summary = result.Value;
            TeacherName = summary.TeacherName;
            TotalClasses = summary.TotalClasses;
            TotalStudents = summary.TotalStudents;
            UngradedAssignments = summary.UngradedAssignments;
            UnreadMessages = summary.UnreadMessages;
            TodaySchedules = summary.TodaySchedules;
            RecentAssignments = summary.RecentAssignments;
            RecentMessages = summary.RecentMessages;
=======
            var data = result.Value!;

            TeacherName = data.TeacherName;
            TotalClasses = data.TotalClasses;
            TotalStudents = data.TotalStudents;
            UngradedAssignments = data.UngradedAssignmentsCount;
            UnreadMessages = data.UnreadMessagesCount;

            TodaySchedules = data.TodaySchedules.Select(s => new TeacherDashboardScheduleDto(
                s.ClassName,
                s.Topic,
                s.TimeRange,
                s.Room
            )).ToList();

            RecentAssignments = data.RecentAssignments.Select(a => new TeacherDashboardAssignmentDto(
                a.Title,
                a.ClassName,
                a.CreatedAt ?? DateTime.Now,
                a.SubmittedCount,
                a.TotalStudents,
                a.TotalStudents > 0 ? (int)((double)a.SubmittedCount / a.TotalStudents * 100) : 0
            )).ToList();

            RecentMessages = data.RecentMessages.Select(m => new TeacherDashboardMessageDto(
                m.SenderName,
                m.SenderRole == "PARENT" ? "Phụ huynh" : m.SenderRole,
                m.ShortContent,
                CalculateTimeAgo(m.SentAt ?? DateTime.Now),
                string.IsNullOrWhiteSpace(m.SenderName) ? "U" : m.SenderName.Substring(0, 1).ToUpper()
            )).ToList();
>>>>>>> e5417bb24ce6b520875746ee3d72982295df8d14

            return Page();
        }

        private string CalculateTimeAgo(DateTime pastTime)
        {
            var span = EduBridge.Helpers.TimeHelper.GetVietnamNow() - pastTime;
            if (span.TotalDays > 1) return $"{(int)span.TotalDays} ngày trước";
            if (span.TotalHours > 1) return $"{(int)span.TotalHours} giờ trước";
            if (span.TotalMinutes > 1) return $"{(int)span.TotalMinutes} phút trước";
            return "Vừa xong";
        }
    }
}
