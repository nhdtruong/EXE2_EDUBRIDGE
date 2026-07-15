using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EduBridge.Contracts.Branches;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Services.Storage;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Services.Branches;

public class BranchManagementService : IBranchManagementService
{
    private readonly AppDbContext _context;
    private readonly IFileStorageService _fileStorageService;

    public BranchManagementService(AppDbContext context, IFileStorageService fileStorageService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
    }

    public async Task<BranchOperationResult<int>> CreateBranchAsync(BranchCreateRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        // Validate unique BranchCode within Center
        var existingBranchCode = await _context.Branches
            .AsNoTracking()
            .AnyAsync(b => b.CenterId == request.CenterId && b.BranchCode == request.BranchCode, cancellationToken);

        if (existingBranchCode)
        {
            errors["BranchCode"] = new List<string> { "Mã cơ sở đã tồn tại trong trung tâm này." };
            return BranchOperationResult<int>.Failure("Dữ liệu không hợp lệ", ToErrors(errors));
        }

        var branch = new Branch
        {
            CenterId = request.CenterId,
            BranchName = request.BranchName,
            BranchCode = request.BranchCode,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            Status = "Active",
            CreatedAt = GetVietnamNow()
        };

        if (request.Logo != null && request.Logo.Length > 0)
        {
            branch.LogoUrl = await _fileStorageService.SaveFileAsync(request.Logo, "branches/logos", cancellationToken);
        }

        if (request.CenterImage != null && request.CenterImage.Length > 0)
        {
            branch.ImageUrl = await _fileStorageService.SaveFileAsync(request.CenterImage, "branches/images", cancellationToken);
        }

        _context.Branches.Add(branch);
        await _context.SaveChangesAsync(cancellationToken);

        return BranchOperationResult<int>.Success(branch.BranchId, "Thêm cơ sở thành công");
    }

    public async Task<BranchDetailResponse?> GetBranchDetailAsync(int branchId, int centerId, CancellationToken cancellationToken = default)
    {
        var branch = await _context.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BranchId == branchId && b.CenterId == centerId, cancellationToken);

        if (branch == null) return null;

        return new BranchDetailResponse
        {
            BranchId = branch.BranchId,
            CenterId = branch.CenterId,
            BranchName = branch.BranchName,
            Email = branch.Email ?? string.Empty,
            PhoneNumber = branch.PhoneNumber ?? string.Empty,
            BranchCode = branch.BranchCode,
            Address = branch.Address ?? string.Empty,
            LogoUrl = branch.LogoUrl,
            ImageUrl = branch.ImageUrl
        };
    }

    public async Task<BranchOperationResult<bool>> UpdateBranchAsync(int branchId, BranchUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        var branch = await _context.Branches
            .FirstOrDefaultAsync(b => b.BranchId == branchId && b.CenterId == request.CenterId, cancellationToken);

        if (branch == null)
        {
            return BranchOperationResult<bool>.Failure("Cơ sở không tồn tại hoặc không có quyền truy cập", new Dictionary<string, string[]>());
        }

        // Validate unique BranchCode within Center
        var existingBranchCode = await _context.Branches
            .AsNoTracking()
            .AnyAsync(b => b.CenterId == request.CenterId && b.BranchCode == request.BranchCode && b.BranchId != branchId, cancellationToken);

        if (existingBranchCode)
        {
            errors["BranchCode"] = new List<string> { "Mã cơ sở đã tồn tại trong trung tâm này." };
            return BranchOperationResult<bool>.Failure("Dữ liệu không hợp lệ", ToErrors(errors));
        }

        branch.BranchName = request.BranchName;
        branch.BranchCode = request.BranchCode;
        branch.Email = request.Email;
        branch.PhoneNumber = request.PhoneNumber;
        branch.Address = request.Address;
        branch.UpdatedAt = GetVietnamNow();

        if (request.Logo != null && request.Logo.Length > 0)
        {
            branch.LogoUrl = await _fileStorageService.SaveFileAsync(request.Logo, "branches/logos", cancellationToken);
        }

        if (request.CenterImage != null && request.CenterImage.Length > 0)
        {
            branch.ImageUrl = await _fileStorageService.SaveFileAsync(request.CenterImage, "branches/images", cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return BranchOperationResult<bool>.Success(true, "Cập nhật cơ sở thành công");
    }

    private static IReadOnlyDictionary<string, string[]> ToErrors(IDictionary<string, List<string>> errors) =>
        errors.ToDictionary(x => x.Key, x => x.Value.ToArray());

    private static DateTime GetVietnamNow()
    {
        try
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        }
        catch
        {
            return DateTime.UtcNow.AddHours(7);
        }
    }
}
