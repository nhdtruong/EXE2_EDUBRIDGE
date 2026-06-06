using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class Enrollment
{
    public int EnrollmentId { get; set; }

    public int StudentId { get; set; }

    public int ClassId { get; set; }

    public DateOnly EnrollDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime StatusChangedAt { get; set; }

    public int? UpdatedByUserId { get; set; }

    public string? Note { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Class Class { get; set; } = null!;

    public virtual ICollection<EnrollmentHistory> EnrollmentHistories { get; set; } = new List<EnrollmentHistory>();

    public virtual Student Student { get; set; } = null!;

    public virtual User? UpdatedByUser { get; set; }
}
