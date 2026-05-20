using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class Class
{
    public int ClassId { get; set; }

    public int CenterId { get; set; }

    public int CourseId { get; set; }

    public int TeacherId { get; set; }

    public string ClassCode { get; set; } = null!;

    public string ClassName { get; set; } = null!;

    public string? Room { get; set; }

    public string? ScheduleText { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public int MaxStudents { get; set; }

    public string Status { get; set; } = null!;

    public virtual Center Center { get; set; } = null!;

    public virtual Course Course { get; set; } = null!;

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

    public virtual Teacher Teacher { get; set; } = null!;
}
