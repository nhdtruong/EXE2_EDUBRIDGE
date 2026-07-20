using EduBridge.Contracts.Classes;

namespace EduBridge.Services.Classes;

public interface IClassCreationService
{
    Task<ClassOperationResult<ClassCreateOptionsResponse>> GetCreateOptionsAsync(
        int ownerUserId,
        int centerId,
        CancellationToken cancellationToken = default);

    Task<ClassOperationResult<ClassCreationResponse>> CreateAsync(
        int ownerUserId,
        CreateClassRequest request,
        CancellationToken cancellationToken = default);

    Task<ClassOperationResult<ClassConflictResponse>> CheckConflictsAsync(
        int ownerUserId,
        CreateClassRequest request,
        CancellationToken cancellationToken = default);
}
