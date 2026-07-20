using EduBridge.Contracts.Shifts;
using EduBridge.Services.Classes;

namespace EduBridge.Services.Shifts;

public interface IShiftManagementService
{
    Task<ClassOperationResult<ShiftPagedResponse>> GetShiftsAsync(
        int ownerUserId,
        ShiftQuery query,
        CancellationToken cancellationToken = default);

    Task<ClassOperationResult<ShiftMutationResponse>> CreateAsync(
        int ownerUserId,
        SaveShiftRequest request,
        CancellationToken cancellationToken = default);

    Task<ClassOperationResult<ShiftMutationResponse>> UpdateAsync(
        int ownerUserId,
        int shiftId,
        SaveShiftRequest request,
        CancellationToken cancellationToken = default);

    Task<ClassOperationResult<ShiftMutationResponse>> SetStatusAsync(
        int ownerUserId,
        int shiftId,
        string status,
        CancellationToken cancellationToken = default);

    Task<ClassOperationResult<bool>> DeleteShiftAsync(
        int ownerUserId,
        int shiftId,
        CancellationToken cancellationToken = default);
}
