using EduBridge.Contracts.Rooms;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Services.Classes;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Services.Rooms;

public class RoomManagementService : IRoomManagementService
{
    private readonly AppDbContext _context;

    public RoomManagementService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ClassOperationResult<RoomPagedResponse>> GetRoomsAsync(
        int ownerUserId,
        RoomQuery query,
        CancellationToken cancellationToken = default)
    {
        var centerId = await GetActiveCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null)
            return ClassOperationResult<RoomPagedResponse>.Failure("Không tìm thấy trung tâm.", new Dictionary<string, string[]>());

        var queryable = _context.Rooms
            .Include(r => r.Classes)
            .Where(r => r.CenterId == centerId.Value && !r.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var search = query.Keyword.Trim();
            queryable = queryable.Where(r =>
                r.RoomName.Contains(search) ||
                r.RoomCode.Contains(search) ||
                (r.Location != null && r.Location.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            queryable = queryable.Where(r => r.Status == query.Status);
        }

        var totalItems = await queryable.CountAsync(cancellationToken);

        var rooms = await queryable
            .OrderBy(r => r.RoomName)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = rooms.Select(r => new RoomListItemDto(
            r.RoomId,
            r.RoomCode,
            r.RoomName,
            r.Capacity,
            r.Location,
            r.Classes.Count,
            r.Classes.Count(c => c.Status == "Active"),
            r.Status,
            r.Classes
                .Where(c => c.Status == "Active")
                .Select(c => {
                    var scheduleText = string.IsNullOrWhiteSpace(c.ScheduleText) ? string.Empty : c.ScheduleText;
                    return new RoomScheduleItemDto(
                        c.ClassName,
                        scheduleText,
                        string.IsNullOrWhiteSpace(scheduleText)
                            ? Array.Empty<string>()
                            : scheduleText.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    );
                })
                .ToList(),
            r.Capacity?.ToString() ?? "-",
            string.IsNullOrWhiteSpace(r.Location) ? "-" : r.Location,
            GetRoomStatusText(r.Status),
            GetRoomStatusBadgeClass(r.Status)
        )).ToList();

        var response = new RoomPagedResponse(
            items,
            totalItems,
            query.PageNumber,
            query.PageSize,
            (int)Math.Ceiling(totalItems / (double)query.PageSize)
        );

        return ClassOperationResult<RoomPagedResponse>.Success(response, "Success");
    }

    public async Task<ClassOperationResult<RoomMutationResponse>> CreateAsync(
        int ownerUserId,
        SaveRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        var centerId = await GetActiveCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null)
            return ClassOperationResult<RoomMutationResponse>.Failure("Không tìm thấy trung tâm.", new Dictionary<string, string[]>());

        var existingRoom = await _context.Rooms
            .AnyAsync(r =>
                r.CenterId == centerId.Value &&
                !r.IsDeleted &&
                r.RoomCode == request.RoomCode,
                cancellationToken);

        if (existingRoom)
        {
            return ClassOperationResult<RoomMutationResponse>.Failure("Mã phòng học đã tồn tại.", new Dictionary<string, string[]>());
        }

        var room = new Room
        {
            CenterId = centerId.Value,
            RoomCode = request.RoomCode,
            RoomName = request.RoomName,
            Capacity = request.Capacity,
            Location = request.Location,
            Status = request.Status,
            IsDeleted = false
        };

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync(cancellationToken);

        return ClassOperationResult<RoomMutationResponse>.Success(
            new RoomMutationResponse(room.RoomId, room.RoomName, room.Status),
            "Thêm phòng học thành công.");
    }

    public async Task<ClassOperationResult<RoomMutationResponse>> UpdateAsync(
        int ownerUserId,
        int roomId,
        SaveRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        var centerId = await GetActiveCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null)
            return ClassOperationResult<RoomMutationResponse>.Failure("Không tìm thấy trung tâm.", new Dictionary<string, string[]>());

        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.RoomId == roomId && r.CenterId == centerId.Value && !r.IsDeleted, cancellationToken);

        if (room == null)
            return ClassOperationResult<RoomMutationResponse>.Failure("Không tìm thấy phòng học.", new Dictionary<string, string[]>());

        if (room.RoomCode != request.RoomCode)
        {
            var existingRoom = await _context.Rooms
                .AnyAsync(r =>
                    r.CenterId == centerId.Value &&
                    !r.IsDeleted &&
                    r.RoomCode == request.RoomCode,
                    cancellationToken);

            if (existingRoom)
            {
                return ClassOperationResult<RoomMutationResponse>.Failure("Mã phòng học đã tồn tại.", new Dictionary<string, string[]>());
            }
        }

        room.RoomCode = request.RoomCode;
        room.RoomName = request.RoomName;
        room.Capacity = request.Capacity;
        room.Location = request.Location;
        room.Status = request.Status;

        await _context.SaveChangesAsync(cancellationToken);

        return ClassOperationResult<RoomMutationResponse>.Success(
            new RoomMutationResponse(room.RoomId, room.RoomName, room.Status),
            "Cập nhật phòng học thành công.");
    }

    public async Task<ClassOperationResult<RoomMutationResponse>> SetStatusAsync(
        int ownerUserId,
        int roomId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var centerId = await GetActiveCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null)
            return ClassOperationResult<RoomMutationResponse>.Failure("Không tìm thấy trung tâm.", new Dictionary<string, string[]>());

        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.RoomId == roomId && r.CenterId == centerId.Value && !r.IsDeleted, cancellationToken);

        if (room == null)
            return ClassOperationResult<RoomMutationResponse>.Failure("Không tìm thấy phòng học.", new Dictionary<string, string[]>());

        if (status == "Inactive" || status == "Maintenance")
        {
            var hasActiveClasses = await _context.Classes
                .AnyAsync(c =>
                    c.RoomId == roomId &&
                    c.Status == "Active",
                    cancellationToken);

            if (hasActiveClasses)
            {
                return ClassOperationResult<RoomMutationResponse>.Failure("Không thể khóa phòng đang có lớp học hoạt động.", new Dictionary<string, string[]>());
            }
        }

        room.Status = status;
        await _context.SaveChangesAsync(cancellationToken);

        return ClassOperationResult<RoomMutationResponse>.Success(
            new RoomMutationResponse(room.RoomId, room.RoomName, room.Status),
            "Cập nhật trạng thái thành công.");
    }

    public async Task<ClassOperationResult<bool>> DeleteRoomAsync(
        int ownerUserId,
        int roomId,
        CancellationToken cancellationToken = default)
    {
        var centerId = await GetActiveCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null)
            return ClassOperationResult<bool>.Failure("Không tìm thấy trung tâm.", new Dictionary<string, string[]>());

        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.RoomId == roomId && r.CenterId == centerId.Value && !r.IsDeleted, cancellationToken);

        if (room == null)
            return ClassOperationResult<bool>.Failure("Không tìm thấy phòng học.", new Dictionary<string, string[]>());

        var hasActiveClasses = await _context.Classes
            .AnyAsync(c =>
                c.RoomId == roomId &&
                c.Status == "Active",
                cancellationToken);

        if (hasActiveClasses)
        {
            return ClassOperationResult<bool>.Failure("Không thể xóa phòng đang có lớp học hoạt động.", new Dictionary<string, string[]>());
        }

        room.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);

        return ClassOperationResult<bool>.Success(true, "Xóa phòng học thành công.");
    }

    private async Task<int?> GetActiveCenterIdAsync(int ownerUserId, CancellationToken cancellationToken)
    {
        return await _context.Centers
            .Where(c => c.OwnerUserId == ownerUserId && c.Status == "Active")
            .Select(c => c.CenterId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string GetRoomStatusText(string status) => status.ToUpperInvariant() switch
    {
        "ACTIVE" => "Đang sử dụng",
        "INACTIVE" => "Tạm dừng",
        "MAINTENANCE" => "Bảo trì",
        _ => "Không xác định"
    };

    private static string GetRoomStatusBadgeClass(string status) => status.ToUpperInvariant() switch
    {
        "ACTIVE" => "bg-green-100 text-green-700",
        "INACTIVE" => "bg-yellow-100 text-yellow-700",
        "MAINTENANCE" => "bg-orange-100 text-orange-700",
        _ => "bg-red-100 text-red-700"
    };
}
