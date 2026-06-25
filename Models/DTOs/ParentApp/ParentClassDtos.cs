using System.Collections.Generic;

namespace EduBridge.Models.DTOs.ParentApp;

public class ParentClassOverviewDto
{
    public int ClassId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ClassCode { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ParentClassAttendanceSummaryDto
{
    public int TotalLessons { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public int ExcusedCount { get; set; }
}

public class ParentClassDetailDto
{
    public int ClassId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ClassCode { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public ParentClassAttendanceSummaryDto AttendanceSummary { get; set; } = new();
    public List<ParentScheduleDto> UpcomingLessons { get; set; } = new();
    public List<ParentAttendanceDto> AttendanceHistory { get; set; } = new();
}
