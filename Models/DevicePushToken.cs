using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class DevicePushToken
{
    public int DevicePushTokenId { get; set; }

    public int UserId { get; set; }

    public string ExpoPushToken { get; set; } = null!;

    public string Platform { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
