using EduBridge.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduBridge.Services.Auth;

public interface ICurrentCenterService
{
    Task<int?> GetCenterIdAsync(CancellationToken cancellationToken = default);
}

public interface ICurrentBranchService
{
    Task<int?> GetBranchIdAsync(CancellationToken cancellationToken = default);
}

public sealed class CurrentCenterService : ICurrentCenterService, ICurrentBranchService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _context;
    private readonly ILogger<CurrentCenterService> _logger;

    public CurrentCenterService(IHttpContextAccessor httpContextAccessor, AppDbContext context, ILogger<CurrentCenterService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
        _logger = logger;
    }

    public async Task<int?> GetCenterIdAsync(CancellationToken cancellationToken = default)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !user.Identity?.IsAuthenticated == true)
        {
            return null;
        }

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return null;
        }

        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

        // SYSTEM_ADMIN use SupportCenterId if set.
        if (roleClaim == "SYSTEM_ADMIN")
        {
            if (_httpContextAccessor.HttpContext != null &&
                _httpContextAccessor.HttpContext.Request.Cookies.TryGetValue("SupportCenterId", out var supportCenterIdStr) &&
                int.TryParse(supportCenterIdStr, out int supportCenterId))
            {
                return supportCenterId;
            }
            
            // If they haven't picked a center to support, return null (meaning they can't access center-specific data)
            return null;
        }

        // For OWNER, TEACHER, PARENT, find their center
        if (roleClaim == "OWNER")
        {
            var centerId = await _context.Centers
                .AsNoTracking()
                .Where(c => c.OwnerUserId == userId && c.Status == "Active")
                .Select(c => (int?)c.CenterId)
                .FirstOrDefaultAsync(cancellationToken);

            if (centerId != null) return centerId;

            return await _context.CenterUsers
                .AsNoTracking()
                .Where(cu => cu.UserId == userId && cu.UserType == "OWNER" && cu.Status == "Active" && cu.Center.Status == "Active")
                .Select(cu => (int?)cu.CenterId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (roleClaim == "TEACHER")
        {
            return await _context.Teachers
                .AsNoTracking()
                .Where(t => t.UserId == userId && !t.IsDeleted && t.Status == "Active")
                .Select(t => (int?)t.CenterId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (roleClaim == "PARENT")
        {
            return await _context.CenterUsers
                .AsNoTracking()
                .Where(cu => cu.UserId == userId && cu.UserType == "PARENT" && cu.Status == "Active" && cu.Center.Status == "Active")
                .Select(cu => (int?)cu.CenterId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    public Task<int?> GetBranchIdAsync(CancellationToken cancellationToken = default)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !user.Identity?.IsAuthenticated == true)
        {
            return Task.FromResult<int?>(null);
        }

        // Support filtering by Branch via QueryString or Cookie for all roles, constrained by Center
        if (_httpContextAccessor.HttpContext != null &&
            _httpContextAccessor.HttpContext.Request.Cookies.TryGetValue("CurrentBranchId", out var currentBranchIdStr) &&
            int.TryParse(currentBranchIdStr, out int branchId))
        {
            return Task.FromResult<int?>(branchId);
        }

        return Task.FromResult<int?>(null);
    }
}
