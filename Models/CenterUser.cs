using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class CenterUser
{
    public int CenterId { get; set; }

    public int UserId { get; set; }

    public string UserType { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public int CenterUserId { get; set; }

    public string Status { get; set; } = null!;

    public string? StaffCode { get; set; }

    public virtual Center Center { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
