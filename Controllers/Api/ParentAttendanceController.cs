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
[Route("api/v1/parent/children/{studentId:int}/attendance")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ParentOnly")]
public class ParentAttendanceController : ControllerBase
{
    private readonly IParentAppService _parentAppService;

    public ParentAttendanceController(IParentAppService parentAppService)
    {
        _parentAppService = parentAppService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ParentAttendanceDto>>>> GetAttendance(int studentId, CancellationToken cancellationToken)
    {
        var parentId = GetUserId();
        if (parentId == 0) return Unauthorized();

        var result = await _parentAppService.GetAttendanceAsync(parentId, studentId, cancellationToken);
        
        return result.IsSuccess
            ? Ok(new ApiResponse<List<ParentAttendanceDto>>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<List<ParentAttendanceDto>>(false, result.Message, null, result.Errors));
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
