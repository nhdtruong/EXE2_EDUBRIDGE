using EduBridge.Contracts.Rooms;
using EduBridge.Services.Classes;

namespace EduBridge.Services.Rooms;

public interface IRoomManagementService
{
    Task<ClassOperationResult<RoomPagedResponse>> GetRoomsAsync(
        int ownerUserId,
        RoomQuery query,
        CancellationToken cancellationToken = default);

    Task<ClassOperationResult<RoomMutationResponse>> CreateAsync(
        int ownerUserId,
        SaveRoomRequest request,
        CancellationToken cancellationToken = default);

    Task<ClassOperationResult<RoomMutationResponse>> UpdateAsync(
        int ownerUserId,
        int roomId,
        SaveRoomRequest request,
        CancellationToken cancellationToken = default);

    Task<ClassOperationResult<RoomMutationResponse>> SetStatusAsync(
        int ownerUserId,
        int roomId,
        string status,
        CancellationToken cancellationToken = default);

    Task<ClassOperationResult<bool>> DeleteRoomAsync(
        int ownerUserId,
        int roomId,
        CancellationToken cancellationToken = default);
}
