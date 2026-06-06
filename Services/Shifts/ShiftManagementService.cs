using EduBridge.Contracts.Shifts;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Services.Classes;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Services.Shifts;

public class ShiftManagementService : IShiftManagementService
{
    private readonly AppDbContext _context;

    public ShiftManagementService(AppDbContext context)
    {
        _context = context;
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
        var centerId = await GetActiveCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null)
            return ClassOperationResult<ShiftMutationResponse>.Failure("Không tìm thấy trung tâm.", new Dictionary<string, string[]>());

        if (request.StartTime >= request.EndTime)
        {
            return ClassOperationResult<ShiftMutationResponse>.Failure("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.", new Dictionary<string, string[]>());
        }

        var existingShift = await _context.StudyShifts
            .AnyAsync(s =>
                s.CenterId == centerId.Value &&
                !s.IsDeleted &&
                s.ShiftCode == request.ShiftCode,
                cancellationToken);

        if (existingShift)
        {
            return ClassOperationResult<ShiftMutationResponse>.Failure("Mã ca học đã tồn tại.", new Dictionary<string, string[]>());
        }

        var shift = new StudyShift
        {
            CenterId = centerId.Value,
            ShiftCode = request.ShiftCode,
            ShiftName = request.ShiftName,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = request.Status,
            Note = request.Note,
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
        var centerId = await GetActiveCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null)
            return ClassOperationResult<ShiftMutationResponse>.Failure("Không tìm thấy trung tâm.", new Dictionary<string, string[]>());

        if (request.StartTime >= request.EndTime)
        {
            return ClassOperationResult<ShiftMutationResponse>.Failure("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.", new Dictionary<string, string[]>());
        }

        var shift = await _context.StudyShifts
            .FirstOrDefaultAsync(s => s.StudyShiftId == shiftId && s.CenterId == centerId.Value && !s.IsDeleted, cancellationToken);

        if (shift == null)
            return ClassOperationResult<ShiftMutationResponse>.Failure("Không tìm thấy ca học.", new Dictionary<string, string[]>());

        if (shift.ShiftCode != request.ShiftCode)
        {
            var existingShift = await _context.StudyShifts
                .AnyAsync(s =>
                    s.CenterId == centerId.Value &&
                    !s.IsDeleted &&
                    s.ShiftCode == request.ShiftCode,
                    cancellationToken);

            if (existingShift)
            {
                return ClassOperationResult<ShiftMutationResponse>.Failure("Mã ca học đã tồn tại.", new Dictionary<string, string[]>());
            }
        }

        shift.ShiftCode = request.ShiftCode;
        shift.ShiftName = request.ShiftName;
        shift.StartTime = request.StartTime;
        shift.EndTime = request.EndTime;
        shift.Status = request.Status;
        shift.Note = request.Note;

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

        if (status == "Inactive")
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

        shift.Status = status;
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
        return await _context.Centers
            .Where(c => c.OwnerUserId == ownerUserId && c.Status == "Active")
            .Select(c => c.CenterId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string GetShiftStatusText(string status) => status.ToUpperInvariant() switch
    {
        "ACTIVE" => "Đang sử dụng",
        "INACTIVE" => "Tạm dừng",
        _ => "Không xác định"
    };
}
