using EduBridge.Contracts.Students;
using EduBridge.Services.Classes;

namespace EduBridge.Services.Students;

public interface IStudentManagementService
{
    Task<ClassOperationResult<StudentPagedResponse>> GetStudentsAsync(int ownerUserId, StudentQuery query, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<StudentResponse>> GetStudentAsync(int ownerUserId, int studentId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<StudentMutationResponse>> CreateStudentAsync(int ownerUserId, SaveStudentRequest request, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<StudentMutationResponse>> UpdateStudentAsync(int ownerUserId, int studentId, UpdateStudentRequest request, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<StudentMutationResponse>> UpdateStudentParentAsync(int ownerUserId, int studentId, UpdateStudentParentRequest request, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<StudentMutationResponse>> ToggleStudentStatusAsync(int ownerUserId, int studentId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<bool>> DeleteStudentAsync(int ownerUserId, int studentId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<List<ParentSearchResultResponse>>> SearchParentsAsync(int ownerUserId, string keyword, CancellationToken cancellationToken = default);
}
