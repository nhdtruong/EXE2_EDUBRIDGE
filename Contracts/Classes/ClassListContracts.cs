namespace EduBridge.Contracts.Classes;

public sealed record ClassQuery(
    string? Keyword,
    string? Status,
    int? CourseId,
    int? TeacherId,
    int PageNumber = 1,
    int PageSize = 10
);

public sealed record ClassPagedResponse(
    IReadOnlyList<ClassListItemDto> Items,
    int TotalItems,
    int PageNumber,
    int PageSize,
    int TotalPages
);

public sealed record ClassListItemDto(
    int ClassId,
    string ClassCode,
    string ClassName,
    string CourseName,
    string TeacherName,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalStudents,
    string ScheduleText,
    IReadOnlyList<string> ScheduleLines,
    string Room,
    string DisplayRoom,
    string StudyPeriod,
    string Status,
    string DisplaySchedule,
    string StatusText,
    string StatusBadgeClass
);

public sealed record ClassDropdownOptionsResponse(
    IReadOnlyList<CourseOptionDto> Courses,
    IReadOnlyList<TeacherOptionDto> Teachers,
    IReadOnlyList<RoomOptionDto> Rooms
);

public sealed record CourseOptionDto(
    int CourseId,
    string CourseName,
    int TotalSessions
);

public sealed record TeacherOptionDto(
    int TeacherId,
    string TeacherName
);

public sealed record RoomOptionDto(
    int RoomId,
    string RoomName,
    string RoomCode,
    string DisplayName
);
