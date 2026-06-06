using EduBridge.Contracts.Classes;

namespace EduBridge.Services.Classes;

public interface IClassManagementService
{
    Task<ClassOperationResult<ClassEditResponse>> GetEditAsync(
        int ownerUserId,
        int classId,
        CancellationToken cancellationToken = default);

    Task<ClassOperationResult<ClassMutationResponse>> UpdateAsync(
        int ownerUserId,
        UpdateClassRequest request,
        CancellationToken cancellationToken = default);

    Task<ClassOperationResult<ClassMutationResponse>> CloseAsync(
        int ownerUserId,
        int classId,
        CancellationToken cancellationToken = default);

    Task<ClassOperationResult<ClassMutationResponse>> SoftDeleteAsync(
        int ownerUserId,
        int classId,
        CancellationToken cancellationToken = default);

    Task<ClassOperationResult<ClassPagedResponse>> GetClassesAsync(
        int ownerUserId,
        ClassQuery query,
        CancellationToken cancellationToken = default);

    Task<ClassOperationResult<ClassDropdownOptionsResponse>> GetClassOptionsAsync(
        int ownerUserId,
        CancellationToken cancellationToken = default);
}
