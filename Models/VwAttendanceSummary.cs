using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class VwAttendanceSummary
{
    public int LessonId { get; set; }

    public string LessonTitle { get; set; } = null!;

    public DateOnly LessonDate { get; set; }

    public string ClassName { get; set; } = null!;

    public int? TotalRecords { get; set; }

    public int? PresentCount { get; set; }

    public int? AbsentCount { get; set; }
}
