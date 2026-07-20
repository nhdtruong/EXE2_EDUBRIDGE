namespace EduBridge.Contracts.Classes;

public sealed record EnrolledStudentResponse(
    int EnrollmentId,
    int StudentId,
    string StudentCode,
    string FullName,
    string? AvatarUrl,
    DateOnly EnrollDate,
    string Status);

public sealed record AvailableStudentResponse(
    int StudentId,
    string StudentCode,
    string FullName,
    string? AvatarUrl,
    string? PhoneNumber);

public sealed record EnrollStudentsResponse(
    int AddedCount,
    int ReactivatedCount,
    IReadOnlyList<EnrolledStudentResponse> Students);
