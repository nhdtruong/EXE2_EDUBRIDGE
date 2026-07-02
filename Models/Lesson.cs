using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class Lesson
{
    public int LessonId { get; set; }

    public int ClassId { get; set; }

    public string LessonTitle { get; set; } = null!;

    public DateOnly LessonDate { get; set; }

    public string? LessonContent { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? SessionNumber { get; set; }

    public int? ClassScheduleId { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Class Class { get; set; } = null!;

    public virtual ClassSchedule? ClassSchedule { get; set; }

    public virtual ICollection<Homework> Homeworks { get; set; } = new List<Homework>();

    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}
