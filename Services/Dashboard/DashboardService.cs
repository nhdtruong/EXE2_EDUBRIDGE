using EduBridge.Contracts.Dashboard;
using EduBridge.Data;
using EduBridge.Services.Classes;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        AppDbContext context,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ClassOperationResult<DashboardSummaryResponse>> GetDashboardSummaryAsync(
        int ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var center = await _context.Centers
            .AsNoTracking()
            .Where(c => c.OwnerUserId == ownerUserId && c.Status == "Active")
            .Select(c => new
            {
                c.CenterId,
                c.CenterName
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (center == null)
        {
            _logger.LogWarning("Owner user {OwnerUserId} has no active center.", ownerUserId);
            return ClassOperationResult<DashboardSummaryResponse>.Failure("Không tìm thấy trung tâm hoạt động.", new Dictionary<string, string[]>());
        }

        var today = GetVietnamToday();
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var previousMonthStart = monthStart.AddMonths(-1);
        var weekStart = today.AddDays(-6);
        var chartStartMonth = monthStart.AddMonths(-5);
        var monthStartDateTime = monthStart.ToDateTime(TimeOnly.MinValue);

        var totalStudents = await _context.Students
            .AsNoTracking()
            .CountAsync(
                s => s.CenterId == center.CenterId &&
                     !s.IsDeleted &&
                     s.Status == "Active",
                cancellationToken);

        var previousMonthStudents = await _context.Students
            .AsNoTracking()
            .CountAsync(
                s =>
                    s.CenterId == center.CenterId &&
                    !s.IsDeleted &&
                    s.Status == "Active" &&
                    s.CreatedAt < monthStartDateTime,
                cancellationToken);

        var activeClasses = await _context.Classes
            .AsNoTracking()
            .CountAsync(
                c => c.CenterId == center.CenterId && c.Status == "Active",
                cancellationToken);

        var previousMonthClasses = await _context.Classes
            .AsNoTracking()
            .CountAsync(
                c =>
                    c.CenterId == center.CenterId &&
                    c.Status == "Active" &&
                    c.StartDate < monthStart,
                cancellationToken);

        var monthlyRevenue = await _context.VwRevenueByPayments
            .AsNoTracking()
            .Where(r =>
                r.CenterId == center.CenterId &&
                r.PaidYear == monthStart.Year &&
                r.PaidMonth == monthStart.Month)
            .SumAsync(r => r.RevenueAmount ?? 0m, cancellationToken);

        var previousMonthlyRevenue = await _context.VwRevenueByPayments
            .AsNoTracking()
            .Where(r =>
                r.CenterId == center.CenterId &&
                r.PaidYear == previousMonthStart.Year &&
                r.PaidMonth == previousMonthStart.Month)
            .SumAsync(r => r.RevenueAmount ?? 0m, cancellationToken);

        var attendanceStats = await _context.Attendances
            .AsNoTracking()
            .Where(a =>
                a.Lesson.Class.CenterId == center.CenterId &&
                a.Lesson.LessonDate >= weekStart &&
                a.Lesson.LessonDate <= today)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Present = g.Count(a => a.Status == "Có mặt")
            })
            .FirstOrDefaultAsync(cancellationToken);

        var weeklyAttendanceRate = attendanceStats == null || attendanceStats.Total == 0
            ? 0m
            : Math.Round(attendanceStats.Present * 100m / attendanceStats.Total, 1);

        var previousAttendanceStats = await _context.Attendances
            .AsNoTracking()
            .Where(a =>
                a.Lesson.Class.CenterId == center.CenterId &&
                a.Lesson.LessonDate >= weekStart.AddDays(-7) &&
                a.Lesson.LessonDate < weekStart)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Present = g.Count(a => a.Status == "Có mặt")
            })
            .FirstOrDefaultAsync(cancellationToken);

        var previousWeeklyAttendanceRate = previousAttendanceStats == null || previousAttendanceStats.Total == 0
            ? 0m
            : Math.Round(previousAttendanceStats.Present * 100m / previousAttendanceStats.Total, 1);

        var latestClasses = await _context.Classes
            .AsNoTracking()
            .Where(c => c.CenterId == center.CenterId && c.Status == "Active")
            .OrderByDescending(c => c.StartDate)
            .ThenByDescending(c => c.ClassId)
            .Take(3)
            .Select(c => new LatestClassDto(
                c.ClassName,
                c.Teacher.User.FullName,
                c.Enrollments.Count(e =>
                    e.Status == "Đang học" &&
                    !e.Student.IsDeleted)
            ))
            .ToListAsync(cancellationToken);

        var importantNotifications = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == ownerUserId)
            .OrderBy(n => n.IsRead)
            .ThenByDescending(n => n.CreatedAt)
            .Take(3)
            .Select(n => new DashboardNotificationDto(
                n.Title,
                n.Content,
                n.IsRead ? "text-blue-500" : "text-red-500"
            ))
            .ToListAsync(cancellationToken);

        var revenueChart = await LoadRevenueChartAsync(
            center.CenterId,
            chartStartMonth,
            monthStart,
            cancellationToken);

        var attendanceChart = await LoadAttendanceChartAsync(
            center.CenterId,
            weekStart,
            today,
            cancellationToken);

        var result = new DashboardSummaryResponse(
            center.CenterName,
            totalStudents,
            FormatCountChange(totalStudents, previousMonthStudents),
            activeClasses,
            FormatCountChange(activeClasses, previousMonthClasses),
            monthlyRevenue,
            FormatPercentChange(monthlyRevenue, previousMonthlyRevenue),
            weeklyAttendanceRate,
            FormatPointChange(weeklyAttendanceRate, previousWeeklyAttendanceRate),
            latestClasses,
            importantNotifications,
            revenueChart,
            attendanceChart
        );

        return ClassOperationResult<DashboardSummaryResponse>.Success(result, "Lấy thông tin tổng quan thành công.");
    }

    private async Task<ChartDataDto> LoadRevenueChartAsync(
        int centerId,
        DateOnly chartStartMonth,
        DateOnly currentMonthStart,
        CancellationToken cancellationToken)
    {
        var startYear = chartStartMonth.Year;
        var startMonth = chartStartMonth.Month;
        var endYear = currentMonthStart.Year;
        var endMonth = currentMonthStart.Month;

        var rows = await _context.VwRevenueByPayments
            .AsNoTracking()
            .Where(r =>
                r.CenterId == centerId &&
                r.PaidYear != null &&
                r.PaidMonth != null &&
                (
                    r.PaidYear > startYear ||
                    (r.PaidYear == startYear && r.PaidMonth >= startMonth)
                ) &&
                (
                    r.PaidYear < endYear ||
                    (r.PaidYear == endYear && r.PaidMonth <= endMonth)
                ))
            .GroupBy(r => new
            {
                Year = r.PaidYear!.Value,
                Month = r.PaidMonth!.Value
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Total = g.Sum(r => r.RevenueAmount ?? 0m)
            })
            .ToListAsync(cancellationToken);

        var labels = new List<string>();
        var values = new List<decimal>();

        for (var month = chartStartMonth; month <= currentMonthStart; month = month.AddMonths(1))
        {
            labels.Add($"Tháng {month.Month}");

            var row = rows.FirstOrDefault(r =>
                r.Year == month.Year &&
                r.Month == month.Month);

            values.Add(row?.Total ?? 0m);
        }

        return new ChartDataDto(labels, values);
    }

    private async Task<AttendanceChartDataDto> LoadAttendanceChartAsync(
        int centerId,
        DateOnly weekStart,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var rows = await _context.Attendances
            .AsNoTracking()
            .Where(a =>
                a.Lesson.Class.CenterId == centerId &&
                a.Lesson.LessonDate >= weekStart &&
                a.Lesson.LessonDate <= today)
            .GroupBy(a => a.Lesson.LessonDate)
            .Select(g => new
            {
                LessonDate = g.Key,
                Present = g.Count(a => a.Status == "Có mặt"),
                Absent = g.Count(a => a.Status == "Vắng")
            })
            .ToListAsync(cancellationToken);

        var labels = new List<string>();
        var presentValues = new List<int>();
        var absentValues = new List<int>();

        for (var date = weekStart; date <= today; date = date.AddDays(1))
        {
            labels.Add(FormatWeekdayLabel(date));

            var row = rows.FirstOrDefault(r => r.LessonDate == date);

            presentValues.Add(row?.Present ?? 0);
            absentValues.Add(row?.Absent ?? 0);
        }

        return new AttendanceChartDataDto(labels, presentValues, absentValues);
    }

    private static string FormatCountChange(int current, int previous)
    {
        var diff = current - previous;

        return diff >= 0
            ? $"+{diff} so với tháng trước"
            : $"{diff} so với tháng trước";
    }

    private static string FormatPercentChange(decimal current, decimal previous)
    {
        if (previous <= 0)
        {
            return current > 0
                ? "Mới phát sinh tháng này"
                : "0% so với tháng trước";
        }

        var percent = Math.Round((current - previous) * 100m / previous, 1);

        return percent >= 0
            ? $"+{percent}% so với tháng trước"
            : $"{percent}% so với tháng trước";
    }

    private static string FormatPointChange(decimal current, decimal previous)
    {
        var diff = Math.Round(current - previous, 1);

        return diff >= 0
            ? $"+{diff}% so với tuần trước"
            : $"{diff}% so với tuần trước";
    }

    private static string FormatWeekdayLabel(DateOnly date)
    {
        var weekday = date.DayOfWeek switch
        {
            DayOfWeek.Monday => "Thứ 2",
            DayOfWeek.Tuesday => "Thứ 3",
            DayOfWeek.Wednesday => "Thứ 4",
            DayOfWeek.Thursday => "Thứ 5",
            DayOfWeek.Friday => "Thứ 6",
            DayOfWeek.Saturday => "Thứ 7",
            _ => "CN"
        };

        return $"{weekday}, {date:dd/MM}";
    }

    private static DateOnly GetVietnamToday()
    {
        TimeZoneInfo timeZone;

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }

        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

        return DateOnly.FromDateTime(vietnamNow);
    }
}
