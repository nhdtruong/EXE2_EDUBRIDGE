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

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Class Class { get; set; } = null!;

    public virtual ICollection<Homework> Homeworks { get; set; } = new List<Homework>();
}
