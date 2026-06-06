using EduBridge.Contracts.Parents;
using EduBridge.Services.Classes;

namespace EduBridge.Services.Parents;

public interface IParentManagementService
{
    Task<ClassOperationResult<ParentPagedResponse>> GetParentsAsync(int ownerUserId, ParentQuery query, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<ParentDetailResponse>> GetParentAsync(int ownerUserId, int parentUserId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<ParentMutationResponse>> CreateAsync(int ownerUserId, SaveParentRequest request, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<ParentMutationResponse>> UpdateAsync(int ownerUserId, int parentUserId, SaveParentRequest request, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<ParentMutationResponse>> SetStatusAsync(int ownerUserId, int parentUserId, string status, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<IReadOnlyList<LinkableStudentResponse>>> GetLinkableStudentsAsync(int ownerUserId, int parentUserId, string? keyword = null, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<bool>> LinkStudentAsync(int ownerUserId, int parentUserId, int studentId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<ResetParentPasswordResponse>> ResetPasswordAsync(int ownerUserId, int parentUserId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<bool>> DeleteParentAsync(int ownerUserId, int parentUserId, CancellationToken cancellationToken = default);
}
