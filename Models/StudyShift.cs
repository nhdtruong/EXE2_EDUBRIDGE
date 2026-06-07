using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class StudyShift
{
    public int StudyShiftId { get; set; }

    public int CenterId { get; set; }

    public string ShiftCode { get; set; } = null!;

    public string ShiftName { get; set; } = null!;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string Status { get; set; } = null!;

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    public virtual Center Center { get; set; } = null!;

    public virtual User? DeletedByUser { get; set; }
}
