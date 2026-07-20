using System;

namespace EduBridge.Models;

public partial class ImportExportHistory
{
    public int HistoryId { get; set; }

    public int CenterId { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = null!;

    public string ActionType { get; set; } = null!;

    public string EntityName { get; set; } = null!;

    public string? InputFileUrl { get; set; }

    public string? ResultFileUrl { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Center Center { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
