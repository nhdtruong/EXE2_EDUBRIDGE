using EduBridge.Contracts.Teachers;
using EduBridge.Services.Classes;

namespace EduBridge.Services.Teachers;

public interface ITeacherManagementService
{
    Task<ClassOperationResult<TeacherPagedResponse>> GetTeachersAsync(int ownerUserId, TeacherQuery query, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<TeacherDetailResponse>> GetTeacherAsync(int ownerUserId, int teacherUserId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<TeacherMutationResponse>> CreateAsync(int ownerUserId, SaveTeacherRequest request, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<TeacherMutationResponse>> UpdateAsync(int ownerUserId, int teacherUserId, SaveTeacherRequest request, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<TeacherMutationResponse>> SetStatusAsync(int ownerUserId, int teacherUserId, string status, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<ResetTeacherPasswordResponse>> ResetPasswordAsync(int ownerUserId, int teacherUserId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<bool>> DeleteTeacherAsync(int ownerUserId, int teacherUserId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<string?>> UpdateAvatarAsync(int ownerUserId, int teacherUserId, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<bool>> RemoveAvatarAsync(int ownerUserId, int teacherUserId, CancellationToken cancellationToken = default);
}
