using System;
using System.Collections.Generic;

namespace EduBridge.Models.DTOs.ParentApp;

public class ParentDashboardDto
{
    public string ParentName { get; set; } = string.Empty;
    public int TotalChildren { get; set; }
    public int TotalClasses { get; set; }
    public int UnreadMessagesCount { get; set; }
    public int UnreadNotificationsCount { get; set; }
    public decimal UnpaidInvoicesTotal { get; set; }
    
    public List<ParentChildOverviewDto> Children { get; set; } = new();
    public List<ParentUpcomingLessonDto> UpcomingLessons { get; set; } = new();
}

public class ParentChildOverviewDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int ActiveClassesCount { get; set; }
}

public class ParentUpcomingLessonDto
{
    public int LessonId { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string LessonTitle { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public DateOnly LessonDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? RoomName { get; set; }
}
