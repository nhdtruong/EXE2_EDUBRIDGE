using System;

namespace EduBridge.DTOs.Centers;

public class CenterDto
{
    public int CenterId { get; set; }
    public string CenterCode { get; set; } = null!;
    public int? OwnerUserId { get; set; }
    public string CenterName { get; set; } = null!;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int? ProjectId { get; set; }
    public string? OwnerFullName { get; set; }
    public string? ProjectName { get; set; }
    public string? Logo { get; set; }
}
