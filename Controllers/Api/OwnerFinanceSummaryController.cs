using EduBridge.Contracts.Finance;
using EduBridge.Services.Finance;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/v1/owner/finance/summary")]
[Authorize(Roles = "OWNER")]
public sealed class OwnerFinanceSummaryController : ControllerBase
{
    private readonly IFinanceSummaryService _financeSummaryService;

    public OwnerFinanceSummaryController(IFinanceSummaryService financeSummaryService)
    {
        _financeSummaryService = financeSummaryService;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult> GetDashboardSummaryAsync([FromQuery] int centerId, [FromQuery] int month, [FromQuery] int year, CancellationToken cancellationToken)
    {
        var result = await _financeSummaryService.GetDashboardSummaryAsync(centerId, month, year, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });
        return Ok(new { success = true, data = result.Value });
    }

    [HttpGet("class-debts")]
    public async Task<ActionResult> GetClassDebtSummariesAsync([FromQuery] int centerId, CancellationToken cancellationToken)
    {
        var result = await _financeSummaryService.GetClassDebtSummariesAsync(centerId, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });
        return Ok(new { success = true, data = result.Value });
    }
}
