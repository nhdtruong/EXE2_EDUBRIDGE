using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class Room
{
    public int RoomId { get; set; }

    public int CenterId { get; set; }

    public string RoomCode { get; set; } = null!;

    public string RoomName { get; set; } = null!;

    public int? Capacity { get; set; }

    public string? Location { get; set; }

    public string? Note { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    public virtual Center Center { get; set; } = null!;

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual User? DeletedByUser { get; set; }
}
