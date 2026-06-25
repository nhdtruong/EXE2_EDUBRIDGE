using System.Security.Claims;
using EduBridge.Contracts.Classes;
using EduBridge.Data;
using EduBridge.Models.DTOs.ParentApp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/v1/parent/devices")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ParentOnly")]
public sealed class ParentDevicesController : ControllerBase
{
    private readonly AppDbContext _context;
    public ParentDevicesController(AppDbContext context) => _context = context;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> Register(ParentDeviceTokenRequest request, CancellationToken cancellationToken)
    {
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        if (!request.ExpoPushToken.StartsWith("ExponentPushToken[") && !request.ExpoPushToken.StartsWith("ExpoPushToken["))
            return BadRequest(new ApiResponse<bool>(false, "Expo push token không hợp lệ.", false));
        await _context.Database.ExecuteSqlInterpolatedAsync($"""
            MERGE DevicePushTokens AS target
            USING (SELECT {request.ExpoPushToken} AS ExpoPushToken) AS source
            ON target.ExpoPushToken = source.ExpoPushToken
            WHEN MATCHED THEN UPDATE SET UserId={userId}, Platform={request.Platform}, IsActive=1, UpdatedAt=SYSDATETIME()
            WHEN NOT MATCHED THEN INSERT(UserId, ExpoPushToken, Platform, IsActive, CreatedAt, UpdatedAt)
            VALUES({userId}, {request.ExpoPushToken}, {request.Platform}, 1, SYSDATETIME(), SYSDATETIME());
            """, cancellationToken);
        return Ok(new ApiResponse<bool>(true, "Đã đăng ký thiết bị.", true));
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResponse<bool>>> Unregister([FromQuery] string expoPushToken, CancellationToken cancellationToken)
    {
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        if (string.IsNullOrWhiteSpace(expoPushToken))
            return BadRequest(new ApiResponse<bool>(false, "Thiếu expo push token.", false));

        await _context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE DevicePushTokens
            SET IsActive = 0,
                UpdatedAt = SYSDATETIME()
            WHERE UserId = {userId}
              AND ExpoPushToken = {expoPushToken};
            """, cancellationToken);

        return Ok(new ApiResponse<bool>(true, "Đã hủy đăng ký thiết bị.", true));
    }
}
