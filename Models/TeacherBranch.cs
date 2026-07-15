using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class TeacherBranch
{
    public int TeacherId { get; set; }
    public int BranchId { get; set; }

    public virtual Branch Branch { get; set; } = null!;
    public virtual Teacher Teacher { get; set; } = null!;
}