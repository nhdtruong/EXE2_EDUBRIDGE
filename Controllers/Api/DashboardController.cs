using System.Security.Claims;
using EduBridge.Contracts.Classes;
using EduBridge.Contracts.Dashboard;
using EduBridge.Models;
using EduBridge.Services.Classes;
using EduBridge.Services.Dashboard;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/v1/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "OWNER")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryResponse>>> GetSummaryAsync(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized(new ApiResponse<DashboardSummaryResponse>(false, "Token không hợp lệ.", null));
        }

        var result = await _dashboardService.GetDashboardSummaryAsync(userId.Value, cancellationToken);

        return result.IsSuccess
            ? Ok(new ApiResponse<DashboardSummaryResponse>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<DashboardSummaryResponse>(false, result.Message, null, result.Errors));
    }

    private int? GetCurrentUserId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;
    }
}
