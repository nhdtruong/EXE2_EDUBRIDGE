using EduBridge.Contracts.Classes;
using EduBridge.Data;
using EduBridge.DTOs.Centers;
using EduBridge.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using EduBridge.Services.Storage;

namespace EduBridge.Services.SystemAdmin;

public class SystemAdminCenterService : ISystemAdminCenterService
{
    private readonly AppDbContext _context;
    private readonly IFileStorageService _fileStorageService;

    public SystemAdminCenterService(AppDbContext context, IFileStorageService fileStorageService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
    }

    public async Task<ApiResponse<CenterDto>> CreateCenterAsync(CreateCenterRequestDto request, int currentUserId, CancellationToken cancellationToken = default)
    {
        // Check if CenterCode already exists
        bool isCodeExist = await _context.Centers.AnyAsync(c => c.CenterCode == request.CenterCode, cancellationToken);
        if (isCodeExist)
        {
            return new ApiResponse<CenterDto>(false, "Mã trung tâm này đã tồn tại trong hệ thống.", null);
        }

        string? logoUrl = null;
        if (request.Logo != null)
        {
            logoUrl = await _fileStorageService.SaveFileAsync(request.Logo, "center-logos", cancellationToken);
        }

        if (!request.ProjectId.HasValue)
        {
            var defaultProjectId = await _context.Projects
                .Where(p => p.ProjectCode == "DEFAULT_PROJ")
                .Select(p => p.ProjectId)
                .FirstOrDefaultAsync(cancellationToken);

            request.ProjectId = defaultProjectId > 0 ? defaultProjectId : 1;
        }

        // Add Center
        var center = new Center
        {
            CenterCode = request.CenterCode,
            CenterName = request.CenterName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            ProjectId = request.ProjectId,
            Logo = logoUrl,
            Status = string.IsNullOrEmpty(request.Status) ? "Active" : request.Status,
            CreatedAt = EduBridge.Helpers.TimeHelper.GetVietnamNow()
        };

        _context.Centers.Add(center);
        await _context.SaveChangesAsync(cancellationToken);

        // Audit Log
        var log = new SystemAuditLog
        {
            ActorUserId = currentUserId,
            TargetCenterId = center.CenterId,
            Action = "CREATE_CENTER",
            EntityName = "Center",
            EntityId = center.CenterId.ToString(),
            CreatedAt = EduBridge.Helpers.TimeHelper.GetVietnamNow()
        };
        _context.SystemAuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new CenterDto
        {
            CenterId = center.CenterId,
            CenterName = center.CenterName,
            Email = center.Email,
            PhoneNumber = center.PhoneNumber,
            Address = center.Address,
            Status = center.Status,
            CreatedAt = center.CreatedAt,
            ProjectId = center.ProjectId,
            Logo = center.Logo
        };

        return new ApiResponse<CenterDto>(true, "Tạo trung tâm thành công", dto);
    }
}
