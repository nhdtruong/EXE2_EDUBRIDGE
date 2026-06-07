using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class VwStudyShiftOverview
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

    public int? ActiveClassCount { get; set; }

    public int? TotalClassCount { get; set; }
}
