using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class Course
{
    public int CourseId { get; set; }

    public int CenterId { get; set; }

    public string CourseName { get; set; } = null!;

    public string? Description { get; set; }

    public int DurationWeeks { get; set; }

    public decimal? TuitionFee { get; set; }

    public string Status { get; set; } = null!;

    public virtual Center Center { get; set; } = null!;

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}
