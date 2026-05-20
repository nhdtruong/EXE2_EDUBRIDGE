using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class VwStudentOverview
{
    public int StudentId { get; set; }

    public string StudentCode { get; set; } = null!;

    public string StudentName { get; set; } = null!;

    public string ParentName { get; set; } = null!;

    public string? ParentPhone { get; set; }

    public string CenterName { get; set; } = null!;

    public string Status { get; set; } = null!;
}
