using EduBridge.Contracts.Classes;
using EduBridge.DTOs.Centers;
using EduBridge.Services.SystemAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/v1/system-admin/centers")]
[Authorize(Policy = "SystemAdminOnly")]
public class SystemAdminCentersController : ControllerBase
{
    private readonly ISystemAdminCenterService _centerService;

    public SystemAdminCentersController(ISystemAdminCenterService centerService)
    {
        _centerService = centerService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CenterDto>>> CreateCenter([FromForm] CreateCenterRequestDto request, CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var currentUserId))
        {
            return Unauthorized(new ApiResponse<CenterDto>(false, "Không tìm thấy thông tin xác thực.", null));
        }

        var result = await _centerService.CreateCenterAsync(request, currentUserId, cancellationToken);
        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }
}
