using System.ComponentModel.DataAnnotations;

namespace EduBridge.Contracts.Classes;

public sealed class CreateClassRequest
{
    [Range(1, int.MaxValue)]
    public int CenterId { get; set; }

    [Required]
    [MaxLength(150)]
    public string ClassName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }

    [Range(1, int.MaxValue)]
    public int TeacherId { get; set; }

    [Range(1, int.MaxValue)]
    public int RoomId { get; set; }

    [Required]
    public DateOnly? StartDate { get; set; }

    [Range(1, 1000)]
    public int? TotalSessions { get; set; }

    [MinLength(1)]
    public List<ClassScheduleRequest> Schedules { get; set; } = new();
}

public sealed class ClassScheduleRequest
{
    [Range(1, 7)]
    public byte DayOfWeek { get; set; }

    public int? StudyShiftId { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }
}
