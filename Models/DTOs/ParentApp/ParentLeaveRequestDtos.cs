namespace EduBridge.Models.DTOs.ParentApp;

public sealed class CreateParentLeaveRequest
{
    public int? LessonId { get; set; }
    public DateOnly? LessonDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class ParentLeaveRequestDto
{
    public int LeaveRequestId { get; set; }
    public int StudentId { get; set; }
    public int LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public DateOnly LessonDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ReviewNote { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ParentDeviceTokenRequest
{
    public string ExpoPushToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
}
