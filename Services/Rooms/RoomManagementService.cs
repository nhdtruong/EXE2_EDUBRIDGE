using EduBridge.Contracts.Rooms;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Services.Classes;
using Microsoft.EntityFrameworkCore;
using EduBridge.Services.Auth;

namespace EduBridge.Services.Rooms;

public class RoomManagementService : IRoomManagementService
{
    private readonly AppDbContext _context;
    private readonly ICurrentCenterService _currentCenterService;

    public RoomManagementService(AppDbContext context, ICurrentCenterService currentCenterService)
    {
        _context = context;
        _currentCenterService = currentCenterService;
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
        CreateRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequest = NormalizeCreateRequest(request);
        var validationErrors = ValidateCreateRequest(normalizedRequest);
        if (validationErrors.Count > 0)
        {
            return ClassOperationResult<RoomMutationResponse>.Failure("Dữ liệu phòng học không hợp lệ.", validationErrors);
        }

        var centerId = await GetActiveCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null)
            return ClassOperationResult<RoomMutationResponse>.Failure("Không tìm thấy trung tâm.", new Dictionary<string, string[]>());

        var existingRoom = await _context.Rooms
            .AnyAsync(r =>
                r.CenterId == centerId.Value &&
                !r.IsDeleted &&
                r.RoomCode == normalizedRequest.RoomCode,
                cancellationToken);

        if (existingRoom)
        {
            return ClassOperationResult<RoomMutationResponse>.Failure(
                "Mã phòng học đã tồn tại.", 
                new Dictionary<string, string[]> { { "RoomCode", new[] { "Mã phòng học đã tồn tại." } } });
        }

        var room = new Room
        {
            CenterId = centerId.Value,
            RoomCode = normalizedRequest.RoomCode,
            RoomName = normalizedRequest.RoomName,
            Capacity = normalizedRequest.Capacity,
            Location = normalizedRequest.Location,
            Status = normalizedRequest.Status,
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
        UpdateRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequest = NormalizeUpdateRequest(request);
        var validationErrors = ValidateUpdateRequest(normalizedRequest);
        if (validationErrors.Count > 0)
        {
            return ClassOperationResult<RoomMutationResponse>.Failure("Dữ liệu phòng học không hợp lệ.", validationErrors);
        }

        var centerId = await GetActiveCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null)
            return ClassOperationResult<RoomMutationResponse>.Failure("Không tìm thấy trung tâm.", new Dictionary<string, string[]>());

        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.RoomId == roomId && r.CenterId == centerId.Value && !r.IsDeleted, cancellationToken);

        if (room == null)
            return ClassOperationResult<RoomMutationResponse>.Failure("Không tìm thấy phòng học.", new Dictionary<string, string[]>());

        if (room.RoomCode != normalizedRequest.RoomCode)
        {
            var existingRoom = await _context.Rooms
                .AnyAsync(r =>
                    r.CenterId == centerId.Value &&
                    !r.IsDeleted &&
                    r.RoomCode == normalizedRequest.RoomCode,
                    cancellationToken);

            if (existingRoom)
            {
                return ClassOperationResult<RoomMutationResponse>.Failure(
                    "Mã phòng học đã tồn tại.", 
                    new Dictionary<string, string[]> { { "RoomCode", new[] { "Mã phòng học đã tồn tại." } } });
            }
        }

        room.RoomCode = normalizedRequest.RoomCode;
        room.RoomName = normalizedRequest.RoomName;
        room.Capacity = normalizedRequest.Capacity;
        room.Location = normalizedRequest.Location;
        room.Status = normalizedRequest.Status;

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

        var normalizedStatus = NormalizeStatus(status);
        if (normalizedStatus == null)
        {
            return ClassOperationResult<RoomMutationResponse>.Failure("Trạng thái phòng học không hợp lệ.", new Dictionary<string, string[]>());
        }

        if (normalizedStatus == "Inactive" || normalizedStatus == "Maintenance")
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

        room.Status = normalizedStatus;
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
        return await _currentCenterService.GetCenterIdAsync(cancellationToken);
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

    private static CreateRoomRequest NormalizeCreateRequest(CreateRoomRequest request)
    {
        return new CreateRoomRequest
        {
            RoomCode = request.RoomCode?.Trim() ?? string.Empty,
            RoomName = request.RoomName?.Trim() ?? string.Empty,
            Capacity = request.Capacity,
            Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(),
            Status = NormalizeStatus(request.Status) ?? string.Empty
        };
    }

    private static Dictionary<string, string[]> ValidateCreateRequest(CreateRoomRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.RoomCode))
        {
            errors["RoomCode"] = new[] { "Vui lòng nhập mã phòng." };
        }
        else if (request.RoomCode.Length > 30)
        {
            errors["RoomCode"] = new[] { "Mã phòng không được vượt quá 30 ký tự." };
        }

        if (string.IsNullOrWhiteSpace(request.RoomName))
        {
            errors["RoomName"] = new[] { "Vui lòng nhập tên phòng." };
        }
        else if (request.RoomName.Length > 100)
        {
            errors["RoomName"] = new[] { "Tên phòng không được vượt quá 100 ký tự." };
        }

        if (request.Capacity is <= 0 or > 10000)
        {
            errors["Capacity"] = new[] { "Sức chứa phải từ 1 đến 10000." };
        }

        if (!string.IsNullOrWhiteSpace(request.Location) && request.Location.Length > 150)
        {
            errors["Location"] = new[] { "Tầng không được vượt quá 150 ký tự." };
        }

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            errors["Status"] = new[] { "Trạng thái phòng học không hợp lệ." };
        }

        return errors;
    }

    private static UpdateRoomRequest NormalizeUpdateRequest(UpdateRoomRequest request)
    {
        return new UpdateRoomRequest
        {
            RoomCode = request.RoomCode?.Trim() ?? string.Empty,
            RoomName = request.RoomName?.Trim() ?? string.Empty,
            Capacity = request.Capacity,
            Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(),
            Status = NormalizeStatus(request.Status) ?? string.Empty
        };
    }

    private static Dictionary<string, string[]> ValidateUpdateRequest(UpdateRoomRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.RoomCode))
        {
            errors["RoomCode"] = new[] { "Vui lòng nhập mã phòng." };
        }
        else if (request.RoomCode.Length > 30)
        {
            errors["RoomCode"] = new[] { "Mã phòng không được vượt quá 30 ký tự." };
        }

        if (string.IsNullOrWhiteSpace(request.RoomName))
        {
            errors["RoomName"] = new[] { "Vui lòng nhập tên phòng." };
        }
        else if (request.RoomName.Length > 100)
        {
            errors["RoomName"] = new[] { "Tên phòng không được vượt quá 100 ký tự." };
        }

        if (request.Capacity is <= 0 or > 10000)
        {
            errors["Capacity"] = new[] { "Sức chứa phải từ 1 đến 10000." };
        }

        if (!string.IsNullOrWhiteSpace(request.Location) && request.Location.Length > 150)
        {
            errors["Location"] = new[] { "Tầng không được vượt quá 150 ký tự." };
        }

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            errors["Status"] = new[] { "Trạng thái phòng học không hợp lệ." };
        }

        return errors;
    }

    private static string? NormalizeStatus(string? status)
    {
        return status?.Trim().ToUpperInvariant() switch
        {
            "ACTIVE" => "Active",
            "INACTIVE" => "Inactive",
            "MAINTENANCE" => "Maintenance",
            _ => null
        };
    }
}
