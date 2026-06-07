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

public sealed record SaveRoomRequest(
    string RoomCode,
    string RoomName,
    int? Capacity,
    string? Location,
    string Status
);

public sealed record RoomMutationResponse(
    int RoomId,
    string RoomName,
    string Status
);

public sealed record RoomStatusRequest(string Status);
