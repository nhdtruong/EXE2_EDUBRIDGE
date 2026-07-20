using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class VwRoomOverview
{
    public int RoomId { get; set; }

    public int CenterId { get; set; }

    public string RoomCode { get; set; } = null!;

    public string RoomName { get; set; } = null!;

    public int? Capacity { get; set; }

    public string? Location { get; set; }

    public string? Note { get; set; }

    public string Status { get; set; } = null!;

    public int? TotalClasses { get; set; }

    public int? ActiveClasses { get; set; }

    public string? LatestClassName { get; set; }

    public string? LatestScheduleText { get; set; }
}
