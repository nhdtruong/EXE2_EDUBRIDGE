using EduBridge.Contracts.Staffs;
using EduBridge.Services.Classes;

namespace EduBridge.Services.Staffs;

public interface IStaffManagementService
{
    Task<ClassOperationResult<StaffPagedResponse>> GetStaffsAsync(int ownerUserId, StaffQuery query, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<StaffDetailResponse>> GetStaffAsync(int ownerUserId, int staffUserId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<StaffMutationResponse>> CreateAsync(int ownerUserId, SaveStaffRequest request, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<StaffMutationResponse>> UpdateAsync(int ownerUserId, int staffUserId, SaveStaffRequest request, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<StaffMutationResponse>> SetStatusAsync(int ownerUserId, int staffUserId, string status, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<ResetStaffPasswordResponse>> ResetPasswordAsync(int ownerUserId, int staffUserId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<bool>> DeleteStaffAsync(int ownerUserId, int staffUserId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<string?>> UpdateAvatarAsync(int ownerUserId, int staffUserId, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<bool>> RemoveAvatarAsync(int ownerUserId, int staffUserId, CancellationToken cancellationToken = default);
}
