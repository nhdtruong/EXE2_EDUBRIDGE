using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class VwStudentOverview
{
    public int StudentId { get; set; }

    public string StudentCode { get; set; } = null!;

    public string StudentName { get; set; } = null!;

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? StudentPhone { get; set; }

    public string? StudentEmail { get; set; }

    public string? AvatarUrl { get; set; }

    public int CenterId { get; set; }

    public string CenterName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int ParentUserId { get; set; }

    public string ParentName { get; set; } = null!;

    public string? ParentPhone { get; set; }

    public string? ParentEmail { get; set; }

    public int? CurrentClassId { get; set; }

    public string? CurrentClassCode { get; set; }

    public string? CurrentClassName { get; set; }

    public string? CurrentCourseName { get; set; }
}
