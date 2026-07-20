using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int LessonId { get; set; }

    public int StudentId { get; set; }

    public string Status { get; set; } = null!;

    public string? Note { get; set; }

    public DateTime RecordedAt { get; set; }

    public virtual Lesson Lesson { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
