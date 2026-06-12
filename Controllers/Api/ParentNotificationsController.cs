using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using EduBridge.Contracts.Classes;
using EduBridge.Models.DTOs.ParentApp;
using EduBridge.Services.ParentApp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/v1/parent/notifications")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ParentOnly")]
public class ParentNotificationsController : ControllerBase
{
    private readonly IParentAppService _parentAppService;

    public ParentNotificationsController(IParentAppService parentAppService)
    {
        _parentAppService = parentAppService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ParentNotificationDto>>>> GetNotifications(CancellationToken cancellationToken)
    {
        var parentId = GetUserId();
        if (parentId == 0) return Unauthorized();

        var result = await _parentAppService.GetNotificationsAsync(parentId, cancellationToken);
        
        return result.IsSuccess
            ? Ok(new ApiResponse<List<ParentNotificationDto>>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<List<ParentNotificationDto>>(false, result.Message, null, result.Errors));
    }

    [HttpPut("{id:int}/read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(int id, CancellationToken cancellationToken)
    {
        var parentId = GetUserId();
        if (parentId == 0) return Unauthorized();

        var result = await _parentAppService.MarkNotificationAsReadAsync(parentId, id, cancellationToken);
        
        return result.IsSuccess
            ? Ok(new ApiResponse<bool>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<bool>(false, result.Message, default, result.Errors));
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
