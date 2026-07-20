namespace EduBridge.Contracts.Classes;

public sealed record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data,
    IReadOnlyDictionary<string, string[]>? Errors = null);

public sealed record ClassCreationResponse(
    int ClassId,
    string ClassCode,
    string ClassName,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalSessions);

public sealed record ClassConflictResponse(
    bool HasConflict,
    IReadOnlyList<ClassConflictItem> Conflicts);

public sealed record ClassConflictItem(
    string ResourceType,
    string ResourceName,
    DateOnly LessonDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string ExistingClassCode,
    string ExistingClassName);

public sealed record ClassCreateOptionsResponse(
    string SuggestedClassCode,
    IReadOnlyList<ClassCourseOption> Courses,
    IReadOnlyList<ClassTeacherOption> Teachers,
    IReadOnlyList<ClassRoomOption> Rooms,
    IReadOnlyList<ClassStudyShiftOption> StudyShifts);

public sealed record ClassCourseOption(int CourseId, string CourseCode, string CourseName, int TotalSessions);

public sealed record ClassTeacherOption(int TeacherId, string TeacherCode, string TeacherName);

public sealed record ClassRoomOption(int RoomId, string RoomCode, string RoomName);

public sealed record ClassStudyShiftOption(
    int StudyShiftId,
    string ShiftCode,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record ClassScheduleResponse(
    int ClassScheduleId,
    byte DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);
