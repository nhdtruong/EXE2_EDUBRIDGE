using EduBridge.Contracts.SystemStaffs;
using EduBridge.Services.SystemStaffs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/v1/system-staffs")]
[Authorize(Policy = "SystemAdminOnly")]
public class SystemStaffsController : ControllerBase
{
    private readonly ISystemStaffService _systemStaffService;
    private readonly ILogger<SystemStaffsController> _logger;

    public SystemStaffsController(ISystemStaffService systemStaffService, ILogger<SystemStaffsController> logger)
    {
        _systemStaffService = systemStaffService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetStaffs([FromQuery] SystemStaffQuery query, CancellationToken cancellationToken)
    {
        var result = await _systemStaffService.GetStaffsAsync(query, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        var response = result.Value!;
        return Ok(new
        {
            success = true,
            data = response.Items,
            pagination = new
            {
                page = response.Page,
                pageSize = response.PageSize,
                totalItems = response.TotalItems,
                totalPages = response.TotalPages
            }
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetStaff(int id, CancellationToken cancellationToken)
    {
        var result = await _systemStaffService.GetStaffAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return NotFound(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, data = result.Value });
    }

    [HttpPost]
    public async Task<IActionResult> CreateStaff([FromBody] SaveSystemStaffRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var result = await _systemStaffService.CreateAsync(currentUserId.Value, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message, data = result.Value });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStaff(int id, [FromBody] SaveSystemStaffRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var result = await _systemStaffService.UpdateAsync(currentUserId.Value, id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message, data = result.Value });
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] string status, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var result = await _systemStaffService.SetStatusAsync(currentUserId.Value, id, status, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = "Cập nhật trạng thái thành công.", data = result.Value });
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var result = await _systemStaffService.ResetPasswordAsync(currentUserId.Value, id, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = "Đặt lại mật khẩu thành công.", data = result.Value });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStaff(int id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var result = await _systemStaffService.DeleteStaffAsync(currentUserId.Value, id, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message });
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
