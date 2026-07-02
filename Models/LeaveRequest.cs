using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class LeaveRequest
{
    public int LeaveRequestId { get; set; }

    public int StudentId { get; set; }

    public int LessonId { get; set; }

    public int ParentUserId { get; set; }

    public string Reason { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? ReviewNote { get; set; }

    public int? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Lesson Lesson { get; set; } = null!;

    public virtual User ParentUser { get; set; } = null!;

    public virtual User? ReviewedByUser { get; set; }

    public virtual Student Student { get; set; } = null!;
}
