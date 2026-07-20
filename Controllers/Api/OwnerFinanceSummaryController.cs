using EduBridge.Contracts.Finance;
using EduBridge.Services.Finance;
using EduBridge.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/v1/owner/finance/summary")]
[Authorize(Policy = "AdminOnly")]
public sealed class OwnerFinanceSummaryController : ControllerBase
{
    private readonly IFinanceSummaryService _financeSummaryService;
    private readonly AppDbContext _context;

    public OwnerFinanceSummaryController(IFinanceSummaryService financeSummaryService, AppDbContext context)
    {
        _financeSummaryService = financeSummaryService;
        _context = context;
    }

    private int? GetCurrentUserId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
    }

    private async Task<bool> VerifyCenterOwnershipAsync(int centerId, int userId)
    {
        var roleClaim = User.FindFirstValue(ClaimTypes.Role);
        if (roleClaim == "SYSTEM_ADMIN" || roleClaim == "PROJECT_ADMIN") return true;

        return await _context.Centers.AnyAsync(c => c.CenterId == centerId && c.OwnerUserId == userId && c.Status == "Active") || 
               await _context.CenterUsers.AnyAsync(cu => cu.CenterId == centerId && cu.UserId == userId && cu.UserType == "OWNER" && cu.Status == "Active");
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult> GetDashboardSummaryAsync([FromQuery] int centerId, [FromQuery] int month, [FromQuery] int year, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (!await VerifyCenterOwnershipAsync(centerId, userId.Value)) return Forbid();

        var result = await _financeSummaryService.GetDashboardSummaryAsync(centerId, month, year, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });
        return Ok(new { success = true, data = result.Value });
    }

    [HttpGet("class-debts")]
    public async Task<ActionResult> GetClassDebtSummariesAsync([FromQuery] int centerId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (!await VerifyCenterOwnershipAsync(centerId, userId.Value)) return Forbid();

        var result = await _financeSummaryService.GetClassDebtSummariesAsync(centerId, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });
        return Ok(new { success = true, data = result.Value });
    }
}
