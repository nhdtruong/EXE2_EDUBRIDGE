using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class VwClassOverview
{
    public int ClassId { get; set; }

    public string ClassCode { get; set; } = null!;

    public string ClassName { get; set; } = null!;

    public string CourseName { get; set; } = null!;

    public string CenterName { get; set; } = null!;

    public string TeacherName { get; set; } = null!;

    public string? ScheduleText { get; set; }

    public string? Room { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string Status { get; set; } = null!;

    public int? TotalStudents { get; set; }
}
