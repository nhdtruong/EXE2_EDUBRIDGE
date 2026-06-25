using EduBridge.Contracts.Shifts;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Services.Classes;
using Microsoft.EntityFrameworkCore;
using EduBridge.Services.Auth;

namespace EduBridge.Services.Shifts;

public class ShiftManagementService : IShiftManagementService
{
    private readonly AppDbContext _context;
    private readonly ICurrentCenterService _currentCenterService;

    public ShiftManagementService(AppDbContext context, ICurrentCenterService currentCenterService)
    {
        _context = context;
        _currentCenterService = currentCenterService;
    }

    public async Task<ClassOperationResult<ShiftPagedResponse>> GetShiftsAsync(
        int ownerUserId,
        ShiftQuery query,
        CancellationToken cancellationToken = default)
    {
        var centerId = await GetActiveCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null)
            return ClassOperationResult<ShiftPagedResponse>.Failure("Không tìm thấy trung tâm.", new Dictionary<string, string[]>());

        var queryable = _context.StudyShifts
            .Where(s => s.CenterId == centerId.Value && !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var search = query.Keyword.Trim();
            queryable = queryable.Where(s =>
                s.ShiftName.Contains(search) ||
                s.ShiftCode.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            queryable = queryable.Where(s => s.Status == query.Status);
        }

        var totalItems = await queryable.CountAsync(cancellationToken);

        var shifts = await queryable
            .OrderBy(s => s.StartTime)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var activeClassCountByShift = await _context.Classes
            .Where(c => c.CenterId == centerId.Value && c.Status == "Active" && !c.IsDeleted)
            .SelectMany(c => c.ClassSchedules)
            .GroupBy(s => new { s.StartTime, s.EndTime })
            .Select(g => new { g.Key.StartTime, g.Key.EndTime, Count = g.Select(s => s.ClassId).Distinct().Count() })
            .ToListAsync(cancellationToken);

        var totalClassCountByShift = await _context.Classes
            .Where(c => c.CenterId == centerId.Value && !c.IsDeleted)
            .SelectMany(c => c.ClassSchedules)
            .GroupBy(s => new { s.StartTime, s.EndTime })
            .Select(g => new { g.Key.StartTime, g.Key.EndTime, Count = g.Select(s => s.ClassId).Distinct().Count() })
            .ToListAsync(cancellationToken);

        var items = shifts.Select(s => new ShiftListItemDto(
            s.StudyShiftId,
            s.ShiftCode,
            s.ShiftName,
            s.StartTime,
            s.EndTime,
            activeClassCountByShift.FirstOrDefault(x => x.StartTime == s.StartTime && x.EndTime == s.EndTime)?.Count ?? 0,
            totalClassCountByShift.FirstOrDefault(x => x.StartTime == s.StartTime && x.EndTime == s.EndTime)?.Count ?? 0,
            s.Status,
            s.Note,
            s.Status == "Active",
            s.StartTime.ToString("HH:mm"),
            s.EndTime.ToString("HH:mm"),
            GetShiftStatusText(s.Status)
        )).ToList();

        var response = new ShiftPagedResponse(
            items,
            totalItems,
            query.PageNumber,
            query.PageSize,
            (int)Math.Ceiling(totalItems / (double)query.PageSize)
        );

        return ClassOperationResult<ShiftPagedResponse>.Success(response, "Success");
    }

    public async Task<ClassOperationResult<ShiftMutationResponse>> CreateAsync(
        int ownerUserId,
        SaveShiftRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequest = NormalizeRequest(request);
        var validationErrors = ValidateRequest(normalizedRequest);
        if (validationErrors.Count > 0)
        {
            return ClassOperationResult<ShiftMutationResponse>.Failure("Dữ liệu ca học không hợp lệ.", validationErrors);
        }

        var centerId = await GetActiveCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null)
            return ClassOperationResult<ShiftMutationResponse>.Failure("Không tìm thấy trung tâm.", new Dictionary<string, string[]>());

        if (normalizedRequest.StartTime >= normalizedRequest.EndTime)
        {
            return ClassOperationResult<ShiftMutationResponse>.Failure(
                "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.",
                new Dictionary<string, string[]> { { "EndTime", new[] { "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc." } } });
        }

        var existingShift = await _context.StudyShifts
            .AnyAsync(s =>
                s.CenterId == centerId.Value &&
                !s.IsDeleted &&
                s.ShiftCode == normalizedRequest.ShiftCode,
                cancellationToken);

        if (existingShift)
        {
            return ClassOperationResult<ShiftMutationResponse>.Failure(
                "Mã ca học đã tồn tại.",
                new Dictionary<string, string[]> { { "ShiftCode", new[] { "Mã ca học đã tồn tại." } } });
        }

        var shift = new StudyShift
        {
            CenterId = centerId.Value,
            ShiftCode = normalizedRequest.ShiftCode,
            ShiftName = normalizedRequest.ShiftName,
            StartTime = normalizedRequest.StartTime,
            EndTime = normalizedRequest.EndTime,
            Status = normalizedRequest.Status,
            Note = normalizedRequest.Note,
            IsDeleted = false
        };

        _context.StudyShifts.Add(shift);
        await _context.SaveChangesAsync(cancellationToken);

        return ClassOperationResult<ShiftMutationResponse>.Success(
            new ShiftMutationResponse(shift.StudyShiftId, shift.ShiftName, shift.Status),
            "Thêm ca học thành công.");
    }

    public async Task<ClassOperationResult<ShiftMutationResponse>> UpdateAsync(
        int ownerUserId,
        int shiftId,
        SaveShiftRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequest = NormalizeRequest(request);
        var validationErrors = ValidateRequest(normalizedRequest);
        if (validationErrors.Count > 0)
        {
            return ClassOperationResult<ShiftMutationResponse>.Failure("Dữ liệu ca học không hợp lệ.", validationErrors);
        }

        var centerId = await GetActiveCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null)
            return ClassOperationResult<ShiftMutationResponse>.Failure("Không tìm thấy trung tâm.", new Dictionary<string, string[]>());

        if (normalizedRequest.StartTime >= normalizedRequest.EndTime)
        {
            return ClassOperationResult<ShiftMutationResponse>.Failure(
                "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.",
                new Dictionary<string, string[]> { { "EndTime", new[] { "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc." } } });
        }

        var shift = await _context.StudyShifts
            .FirstOrDefaultAsync(s => s.StudyShiftId == shiftId && s.CenterId == centerId.Value && !s.IsDeleted, cancellationToken);

        if (shift == null)
            return ClassOperationResult<ShiftMutationResponse>.Failure("Không tìm thấy ca học.", new Dictionary<string, string[]>());

        if (shift.ShiftCode != normalizedRequest.ShiftCode)
        {
            var existingShift = await _context.StudyShifts
                .AnyAsync(s =>
                    s.CenterId == centerId.Value &&
                    !s.IsDeleted &&
                    s.ShiftCode == normalizedRequest.ShiftCode,
                    cancellationToken);

            if (existingShift)
            {
                return ClassOperationResult<ShiftMutationResponse>.Failure(
                    "Mã ca học đã tồn tại.",
                    new Dictionary<string, string[]> { { "ShiftCode", new[] { "Mã ca học đã tồn tại." } } });
            }
        }

        shift.ShiftCode = normalizedRequest.ShiftCode;
        shift.ShiftName = normalizedRequest.ShiftName;
        shift.StartTime = normalizedRequest.StartTime;
        shift.EndTime = normalizedRequest.EndTime;
        shift.Status = normalizedRequest.Status;
        shift.Note = normalizedRequest.Note;

        await _context.SaveChangesAsync(cancellationToken);

        return ClassOperationResult<ShiftMutationResponse>.Success(
            new ShiftMutationResponse(shift.StudyShiftId, shift.ShiftName, shift.Status),
            "Cập nhật ca học thành công.");
    }

    public async Task<ClassOperationResult<ShiftMutationResponse>> SetStatusAsync(
        int ownerUserId,
        int shiftId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var centerId = await GetActiveCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null)
            return ClassOperationResult<ShiftMutationResponse>.Failure("Không tìm thấy trung tâm.", new Dictionary<string, string[]>());

        var shift = await _context.StudyShifts
            .FirstOrDefaultAsync(s => s.StudyShiftId == shiftId && s.CenterId == centerId.Value && !s.IsDeleted, cancellationToken);

        if (shift == null)
            return ClassOperationResult<ShiftMutationResponse>.Failure("Không tìm thấy ca học.", new Dictionary<string, string[]>());

        var normalizedStatus = NormalizeStatus(status);
        if (normalizedStatus == null)
        {
            return ClassOperationResult<ShiftMutationResponse>.Failure("Trạng thái ca học không hợp lệ.", new Dictionary<string, string[]>());
        }

        if (normalizedStatus == "Inactive")
        {
            var hasActiveClasses = await _context.Classes
                .AnyAsync(c =>
                    c.CenterId == centerId.Value &&
                    c.Status == "Active" &&
                    !c.IsDeleted &&
                    c.ClassSchedules.Any(cs => cs.StartTime == shift.StartTime && cs.EndTime == shift.EndTime),
                    cancellationToken);

            if (hasActiveClasses)
            {
                return ClassOperationResult<ShiftMutationResponse>.Failure("Không thể khóa ca học đang có lớp học hoạt động sử dụng.", new Dictionary<string, string[]>());
            }
        }

        shift.Status = normalizedStatus;
        await _context.SaveChangesAsync(cancellationToken);

        return ClassOperationResult<ShiftMutationResponse>.Success(
            new ShiftMutationResponse(shift.StudyShiftId, shift.ShiftName, shift.Status),
            "Cập nhật trạng thái thành công.");
    }

    public async Task<ClassOperationResult<bool>> DeleteShiftAsync(
        int ownerUserId,
        int shiftId,
        CancellationToken cancellationToken = default)
    {
        var centerId = await GetActiveCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null)
            return ClassOperationResult<bool>.Failure("Không tìm thấy trung tâm.", new Dictionary<string, string[]>());

        var shift = await _context.StudyShifts
            .FirstOrDefaultAsync(s => s.StudyShiftId == shiftId && s.CenterId == centerId.Value && !s.IsDeleted, cancellationToken);

        if (shift == null)
            return ClassOperationResult<bool>.Failure("Không tìm thấy ca học.", new Dictionary<string, string[]>());

        var hasActiveClasses = await _context.Classes
            .AnyAsync(c =>
                c.CenterId == centerId.Value &&
                c.Status == "Active" &&
                !c.IsDeleted &&
                c.ClassSchedules.Any(cs => cs.StartTime == shift.StartTime && cs.EndTime == shift.EndTime),
                cancellationToken);

        if (hasActiveClasses)
        {
            return ClassOperationResult<bool>.Failure("Không thể xóa ca học đang có lớp học hoạt động sử dụng.", new Dictionary<string, string[]>());
        }

        shift.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);

        return ClassOperationResult<bool>.Success(true, "Xóa ca học thành công.");
    }

    private async Task<int?> GetActiveCenterIdAsync(int ownerUserId, CancellationToken cancellationToken)
    {
        return await _currentCenterService.GetCenterIdAsync(cancellationToken);
    }

    private static string GetShiftStatusText(string status) => status.ToUpperInvariant() switch
    {
        "ACTIVE" => "Đang sử dụng",
        "INACTIVE" => "Tạm dừng",
        _ => "Không xác định"
    };

    private static SaveShiftRequest NormalizeRequest(SaveShiftRequest request)
    {
        return new SaveShiftRequest
        {
            ShiftCode = request.ShiftCode?.Trim() ?? string.Empty,
            ShiftName = request.ShiftName?.Trim() ?? string.Empty,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = NormalizeStatus(request.Status) ?? string.Empty,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()
        };
    }

    private static Dictionary<string, string[]> ValidateRequest(SaveShiftRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.ShiftCode))
        {
            errors["ShiftCode"] = new[] { "Vui lòng nhập mã ca." };
        }
        else if (request.ShiftCode.Length > 30)
        {
            errors["ShiftCode"] = new[] { "Mã ca không được vượt quá 30 ký tự." };
        }

        if (string.IsNullOrWhiteSpace(request.ShiftName))
        {
            errors["ShiftName"] = new[] { "Vui lòng nhập tên ca." };
        }
        else if (request.ShiftName.Length > 100)
        {
            errors["ShiftName"] = new[] { "Tên ca không được vượt quá 100 ký tự." };
        }

        if (request.StartTime == default)
        {
            errors["StartTime"] = new[] { "Vui lòng chọn giờ bắt đầu." };
        }

        if (request.EndTime == default)
        {
            errors["EndTime"] = new[] { "Vui lòng chọn giờ kết thúc." };
        }
        else if (request.StartTime != default && request.EndTime <= request.StartTime)
        {
            errors["EndTime"] = new[] { "Giờ kết thúc phải sau giờ bắt đầu." };
        }

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            errors["Status"] = new[] { "Trạng thái ca học không hợp lệ." };
        }

        if (!string.IsNullOrWhiteSpace(request.Note) && request.Note.Length > 255)
        {
            errors["Note"] = new[] { "Ghi chú không được vượt quá 255 ký tự." };
        }

        return errors;
    }

    private static string? NormalizeStatus(string? status)
    {
        return status?.Trim().ToUpperInvariant() switch
        {
            "ACTIVE" => "Active",
            "INACTIVE" => "Inactive",
            _ => null
        };
    }
}
