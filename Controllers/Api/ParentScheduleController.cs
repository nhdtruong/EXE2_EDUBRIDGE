using System;
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
[Route("api/v1/parent/schedule")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ParentOnly")]
public class ParentScheduleController : ControllerBase
{
    private readonly IParentAppService _parentAppService;

    public ParentScheduleController(IParentAppService parentAppService)
    {
        _parentAppService = parentAppService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ParentScheduleDto>>>> GetSchedule(
        [FromQuery] int? studentId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var parentId = GetUserId();
        if (parentId == 0) return Unauthorized();

        var result = await _parentAppService.GetScheduleAsync(parentId, studentId, fromDate, toDate, cancellationToken);
        
        return result.IsSuccess
            ? Ok(new ApiResponse<List<ParentScheduleDto>>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<List<ParentScheduleDto>>(false, result.Message, null, result.Errors));
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
