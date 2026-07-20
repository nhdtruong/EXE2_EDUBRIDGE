using System.Security.Claims;
using EduBridge.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminDashboardModel : PageModel
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<AdminDashboardModel> _logger;

        public AdminDashboardModel(
            IDashboardService dashboardService,
            ILogger<AdminDashboardModel> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        public OwnerDashboardViewModel Dashboard { get; private set; } = OwnerDashboardViewModel.Empty();

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            var ownerUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(ownerUserIdValue, out var ownerUserId))
            {
                return RedirectToPage("/Login");
            }

            var result = await _dashboardService.GetDashboardSummaryAsync(ownerUserId, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                var summary = result.Value;
                
                Dashboard = new OwnerDashboardViewModel
                {
                    CenterName = summary.CenterName,
                    TotalStudents = summary.TotalStudents,
                    StudentChangeText = summary.StudentChangeText,
                    ActiveClasses = summary.ActiveClasses,
                    ClassChangeText = summary.ClassChangeText,
                    MonthlyRevenue = summary.MonthlyRevenue,
                    RevenueChangeText = summary.RevenueChangeText,
                    WeeklyAttendanceRate = summary.WeeklyAttendanceRate,
                    AttendanceChangeText = summary.AttendanceChangeText,
                    LatestClasses = summary.LatestClasses.Select(c => new LatestClassItem
                    {
                        ClassName = c.ClassName,
                        TeacherName = c.TeacherName,
                        TotalStudents = c.TotalStudents
                    }).ToList(),
                    ImportantNotifications = summary.ImportantNotifications.Select(n => new DashboardNotificationItem
                    {
                        Title = n.Title,
                        Content = n.Content,
                        LevelCssClass = n.LevelCssClass
                    }).ToList(),
                    RevenueChartLabels = summary.RevenueChart.Labels.ToList(),
                    RevenueChartValues = summary.RevenueChart.Values.ToList(),
                    AttendanceChartLabels = summary.AttendanceChart.Labels.ToList(),
                    PresentChartValues = summary.AttendanceChart.PresentValues.ToList(),
                    AbsentChartValues = summary.AttendanceChart.AbsentValues.ToList()
                };
            }
            else
            {
                _logger.LogWarning("Owner user {OwnerUserId} dashboard loading failed: {Message}", ownerUserId, result.Message);
            }

            return Page();
        }
    }

    public class OwnerDashboardViewModel
    {
        public string CenterName { get; set; } = string.Empty;

        public int TotalStudents { get; set; }

        public string StudentChangeText { get; set; } = "0 so với tháng trước";

        public int ActiveClasses { get; set; }

        public string ClassChangeText { get; set; } = "0 so với tháng trước";

        public decimal MonthlyRevenue { get; set; }

        public string RevenueChangeText { get; set; } = "0% so với tháng trước";

        public decimal WeeklyAttendanceRate { get; set; }

        public string AttendanceChangeText { get; set; } = "0% so với tuần trước";

        public List<LatestClassItem> LatestClasses { get; set; } = new();

        public List<DashboardNotificationItem> ImportantNotifications { get; set; } = new();

        public List<string> RevenueChartLabels { get; set; } = new();

        public List<decimal> RevenueChartValues { get; set; } = new();

        public List<string> AttendanceChartLabels { get; set; } = new();

        public List<int> PresentChartValues { get; set; } = new();

        public List<int> AbsentChartValues { get; set; } = new();

        public static OwnerDashboardViewModel Empty()
        {
            return new OwnerDashboardViewModel
            {
                CenterName = "Chưa có trung tâm"
            };
        }
    }

    public class LatestClassItem
    {
        public string ClassName { get; set; } = string.Empty;

        public string TeacherName { get; set; } = string.Empty;

        public int TotalStudents { get; set; }
    }

    public class DashboardNotificationItem
    {
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string LevelCssClass { get; set; } = "text-blue-500";
    }
}
