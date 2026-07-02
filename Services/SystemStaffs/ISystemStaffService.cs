using EduBridge.Contracts.SystemStaffs;
using EduBridge.Services.Classes;

namespace EduBridge.Services.SystemStaffs;

public interface ISystemStaffService
{
    Task<ClassOperationResult<SystemStaffPagedResponse>> GetStaffsAsync(SystemStaffQuery query, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<SystemStaffDetailResponse>> GetStaffAsync(int staffUserId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<SystemStaffMutationResponse>> CreateAsync(int currentUserId, SaveSystemStaffRequest request, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<SystemStaffMutationResponse>> UpdateAsync(int currentUserId, int staffUserId, SaveSystemStaffRequest request, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<SystemStaffMutationResponse>> SetStatusAsync(int currentUserId, int staffUserId, string status, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<SystemStaffMutationResponse>> UpdateAvatarAsync(int currentUserId, int staffUserId, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<bool>> RemoveAvatarAsync(int currentUserId, int staffUserId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<ResetSystemStaffPasswordResponse>> ResetPasswordAsync(int currentUserId, int staffUserId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<bool>> DeleteStaffAsync(int currentUserId, int staffUserId, CancellationToken cancellationToken = default);
}
