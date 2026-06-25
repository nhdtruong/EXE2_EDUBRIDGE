using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class Teacher
{
    public int TeacherId { get; set; }

    public int UserId { get; set; }

    public int CenterId { get; set; }

    public string? Specialization { get; set; }

    public int ExperienceYears { get; set; }

    public string Status { get; set; } = null!;

    public string TeacherCode { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    public int? BranchId { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual Center Center { get; set; } = null!;

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual User User { get; set; } = null!;
}
