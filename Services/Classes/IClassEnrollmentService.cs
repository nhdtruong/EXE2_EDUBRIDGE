using EduBridge.Contracts.Classes;

namespace EduBridge.Services.Classes;

public interface IClassEnrollmentService
{
    Task<ClassOperationResult<IReadOnlyList<EnrolledStudentResponse>>> GetEnrolledStudentsAsync(
        int ownerUserId,
        int classId,
        CancellationToken cancellationToken = default);

    Task<ClassOperationResult<IReadOnlyList<AvailableStudentResponse>>> GetAvailableStudentsAsync(
        int ownerUserId,
        int classId,
        string? keyword = null,
        CancellationToken cancellationToken = default);

    Task<ClassOperationResult<EnrollStudentsResponse>> EnrollStudentsAsync(
        int ownerUserId,
        int classId,
        EnrollStudentRequest request,
        CancellationToken cancellationToken = default);

    Task<ClassOperationResult<bool>> RemoveStudentAsync(
        int ownerUserId,
        int classId,
        int studentId,
        string? note = null,
        CancellationToken cancellationToken = default);
}
