using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class EnrollmentHistory
{
    public long EnrollmentHistoryId { get; set; }

    public int EnrollmentId { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = null!;

    public DateTime ChangedAt { get; set; }

    public int? ChangedByUserId { get; set; }

    public string? Note { get; set; }

    public virtual User? ChangedByUser { get; set; }

    public virtual Enrollment Enrollment { get; set; } = null!;
}
