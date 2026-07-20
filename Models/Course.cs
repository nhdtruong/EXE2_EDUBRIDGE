using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class Course
{
    public int CourseId { get; set; }

    public int CenterId { get; set; }

    public string CourseName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal? TuitionFee { get; set; }

    public string Status { get; set; } = null!;

    public string CourseCode { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    public int TotalSessions { get; set; }

    public virtual Center Center { get; set; } = null!;

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual User? DeletedByUser { get; set; }
}
