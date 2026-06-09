using System.ComponentModel.DataAnnotations;

namespace EduBridge.Contracts.Rooms;

public sealed record RoomQuery(
    string? Keyword,
    string? Status,
    int PageNumber = 1,
    int PageSize = 10
);

public sealed record RoomPagedResponse(
    IReadOnlyList<RoomListItemDto> Items,
    int TotalItems,
    int PageNumber,
    int PageSize,
    int TotalPages
);

public sealed record RoomListItemDto(
    int RoomId,
    string RoomCode,
    string RoomName,
    int? Capacity,
    string? Location,
    int TotalClasses,
    int ActiveClasses,
    string Status,
    IReadOnlyList<RoomScheduleItemDto> ScheduleItems,
    string DisplayCapacity,
    string DisplayLocation,
    string StatusText,
    string StatusBadgeClass
);

public sealed record RoomScheduleItemDto(
    string ClassName,
    string ScheduleText,
    IReadOnlyList<string> ScheduleLines
);

public sealed class CreateRoomRequest
{
    [Required(ErrorMessage = "Vui lòng nhập mã phòng.")]
    [MaxLength(30, ErrorMessage = "Mã phòng không được vượt quá 30 ký tự.")]
    public string RoomCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên phòng.")]
    [MaxLength(100, ErrorMessage = "Tên phòng không được vượt quá 100 ký tự.")]
    public string RoomName { get; set; } = string.Empty;

    [Range(1, 10000, ErrorMessage = "Sức chứa phải từ 1 đến 10000.")]
    public int? Capacity { get; set; }

    [MaxLength(150, ErrorMessage = "Tầng không được vượt quá 150 ký tự.")]
    public string? Location { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái phòng.")]
    public string Status { get; set; } = string.Empty;

    public CreateRoomRequest() { }

    public CreateRoomRequest(string roomCode, string roomName, int? capacity, string? location, string status)
    {
        RoomCode = roomCode;
        RoomName = roomName;
        Capacity = capacity;
        Location = location;
        Status = status;
    }
}

public sealed class UpdateRoomRequest
{
    [Required(ErrorMessage = "Vui lòng nhập mã phòng.")]
    [MaxLength(30, ErrorMessage = "Mã phòng không được vượt quá 30 ký tự.")]
    public string RoomCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên phòng.")]
    [MaxLength(100, ErrorMessage = "Tên phòng không được vượt quá 100 ký tự.")]
    public string RoomName { get; set; } = string.Empty;

    [Range(1, 10000, ErrorMessage = "Sức chứa phải từ 1 đến 10000.")]
    public int? Capacity { get; set; }

    [MaxLength(150, ErrorMessage = "Tầng không được vượt quá 150 ký tự.")]
    public string? Location { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái phòng.")]
    public string Status { get; set; } = string.Empty;

    public UpdateRoomRequest() { }

    public UpdateRoomRequest(string roomCode, string roomName, int? capacity, string? location, string status)
    {
        RoomCode = roomCode;
        RoomName = roomName;
        Capacity = capacity;
        Location = location;
        Status = status;
    }
}

public sealed record RoomMutationResponse(
    int RoomId,
    string RoomName,
    string Status
);

public sealed record RoomStatusRequest(string Status);
