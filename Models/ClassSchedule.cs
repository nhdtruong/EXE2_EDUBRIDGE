using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class ClassSchedule
{
    public int ClassScheduleId { get; set; }

    public int ClassId { get; set; }

    public byte DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Class Class { get; set; } = null!;
}
