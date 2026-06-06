namespace EduBridge.Contracts.Shifts;

public sealed record ShiftQuery(
    string? Keyword,
    string? Status,
    int PageNumber = 1,
    int PageSize = 10
);

public sealed record ShiftPagedResponse(
    IReadOnlyList<ShiftListItemDto> Items,
    int TotalItems,
    int PageNumber,
    int PageSize,
    int TotalPages
);

public sealed record ShiftListItemDto(
    int StudyShiftId,
    string ShiftCode,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int ActiveClassCount,
    int TotalClassCount,
    string Status,
    string? Note,
    bool IsActive,
    string StartTimeText,
    string EndTimeText,
    string StatusText
);

public sealed record SaveShiftRequest(
    string ShiftCode,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Status,
    string? Note
);

public sealed record ShiftMutationResponse(
    int StudyShiftId,
    string ShiftName,
    string Status
);

public sealed record ShiftStatusRequest(string Status);
