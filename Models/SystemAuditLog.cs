using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class SystemAuditLog
{
    public int LogId { get; set; }

    public int ActorUserId { get; set; }

    public int? TargetCenterId { get; set; }

    public int? TargetProjectId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityName { get; set; } = null!;

    public string EntityId { get; set; } = null!;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? IpAddress { get; set; }

    public virtual User ActorUser { get; set; } = null!;

    public virtual Center? TargetCenter { get; set; }

    public virtual Project? TargetProject { get; set; }
}
