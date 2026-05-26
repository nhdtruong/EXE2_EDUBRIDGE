using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class ClassCodeCounter
{
    public int CenterId { get; set; }

    public string YearMonth { get; set; } = null!;

    public int LastNumber { get; set; }

    public virtual Center Center { get; set; } = null!;
}
