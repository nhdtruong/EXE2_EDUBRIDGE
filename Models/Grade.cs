using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class Grade
{
    public int GradeId { get; set; }

    public int StudentId { get; set; }

    public int ClassId { get; set; }

    public string GradeName { get; set; } = null!;

    public decimal Score { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
