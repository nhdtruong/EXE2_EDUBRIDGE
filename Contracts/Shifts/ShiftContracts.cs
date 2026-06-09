using System.ComponentModel.DataAnnotations;

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

public sealed class SaveShiftRequest
{
    [Required(ErrorMessage = "Vui lòng nhập mã ca.")]
    [MaxLength(30, ErrorMessage = "Mã ca không được vượt quá 30 ký tự.")]
    public string ShiftCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên ca.")]
    [MaxLength(100, ErrorMessage = "Tên ca không được vượt quá 100 ký tự.")]
    public string ShiftName { get; set; } = string.Empty;

    public TimeOnly StartTime { get; set; }
    
    public TimeOnly EndTime { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái ca.")]
    public string Status { get; set; } = string.Empty;

    [MaxLength(255, ErrorMessage = "Ghi chú không được vượt quá 255 ký tự.")]
    public string? Note { get; set; }

    public SaveShiftRequest() { }

    public SaveShiftRequest(string shiftCode, string shiftName, TimeOnly startTime, TimeOnly endTime, string status, string? note)
    {
        ShiftCode = shiftCode;
        ShiftName = shiftName;
        StartTime = startTime;
        EndTime = endTime;
        Status = status;
        Note = note;
    }
}

public sealed record ShiftMutationResponse(
    int StudyShiftId,
    string ShiftName,
    string Status
);

public sealed record ShiftStatusRequest(string Status);
