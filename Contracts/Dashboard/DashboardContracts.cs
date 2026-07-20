namespace EduBridge.Contracts.Dashboard;

public sealed record DashboardSummaryResponse(
    string CenterName,
    int TotalStudents,
    string StudentChangeText,
    int ActiveClasses,
    string ClassChangeText,
    decimal MonthlyRevenue,
    string RevenueChangeText,
    decimal WeeklyAttendanceRate,
    string AttendanceChangeText,
    IReadOnlyList<LatestClassDto> LatestClasses,
    IReadOnlyList<DashboardNotificationDto> ImportantNotifications,
    ChartDataDto RevenueChart,
    AttendanceChartDataDto AttendanceChart
);

public sealed record LatestClassDto(
    string ClassName,
    string TeacherName,
    int TotalStudents
);

public sealed record DashboardNotificationDto(
    string Title,
    string Content,
    string LevelCssClass
);

public sealed record ChartDataDto(
    IReadOnlyList<string> Labels,
    IReadOnlyList<decimal> Values
);

public sealed record AttendanceChartDataDto(
    IReadOnlyList<string> Labels,
    IReadOnlyList<int> PresentValues,
    IReadOnlyList<int> AbsentValues
);
