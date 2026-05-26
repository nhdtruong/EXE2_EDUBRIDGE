using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class Center
{
    public int CenterId { get; set; }

    public int OwnerUserId { get; set; }

    public string CenterName { get; set; } = null!;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ClassCodeCounter> ClassCodeCounters { get; set; } = new List<ClassCodeCounter>();

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    public virtual User OwnerUser { get; set; } = null!;

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual TeacherCodeCounter? TeacherCodeCounter { get; set; }

    public virtual ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
}
