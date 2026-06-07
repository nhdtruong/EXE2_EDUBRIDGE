using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class Class
{
    public int ClassId { get; set; }

    public int CenterId { get; set; }

    public int CourseId { get; set; }

    public int TeacherId { get; set; }

    public string ClassName { get; set; } = null!;

    public string? Room { get; set; }

    public string? ScheduleText { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string Status { get; set; } = null!;

    public string ClassCode { get; set; } = null!;

    public int RoomId { get; set; }

    public int TotalSessions { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedByUserId { get; set; }

    public DateTime? ClosedAt { get; set; }

    public int? ClosedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Center Center { get; set; } = null!;

    public virtual ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();

    public virtual User? ClosedByUser { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual User? DeletedByUser { get; set; }

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

    public virtual Room RoomNavigation { get; set; } = null!;

    public virtual Teacher Teacher { get; set; } = null!;

    public virtual User? UpdatedByUser { get; set; }
}
