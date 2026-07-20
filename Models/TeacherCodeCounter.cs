using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class TeacherCodeCounter
{
    public int CenterId { get; set; }

    public int LastNumber { get; set; }

    public virtual Center Center { get; set; } = null!;
}
