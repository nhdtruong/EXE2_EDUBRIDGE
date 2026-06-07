using System.ComponentModel.DataAnnotations;

namespace EduBridge.Contracts.Classes;

public sealed class UpdateClassRequest
{
    [Range(1, int.MaxValue)]
    public int ClassId { get; set; }

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
    public int TotalSessions { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    [MinLength(1)]
    public List<ClassScheduleRequest> Schedules { get; set; } = new();
}

public sealed record ClassEditResponse(
    int ClassId,
    int CenterId,
    string ClassCode,
    string ClassName,
    int CourseId,
    int TeacherId,
    int RoomId,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalSessions,
    string Status,
    string RowVersion,
    IReadOnlyList<ClassScheduleRequest> Schedules,
    ClassCreateOptionsResponse Options);

public sealed record ClassMutationResponse(int ClassId, string Status);
