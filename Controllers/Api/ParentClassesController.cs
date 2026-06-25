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
[Route("api/v1/parent/classes")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ParentOnly")]
public class ParentClassesController : ControllerBase
{
    private readonly IParentAppService _parentAppService;

    public ParentClassesController(IParentAppService parentAppService)
    {
        _parentAppService = parentAppService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ParentClassOverviewDto>>>> GetClasses(
        [FromQuery] int studentId,
        CancellationToken cancellationToken)
    {
        var parentId = GetUserId();
        if (parentId == 0) return Unauthorized();

        var result = await _parentAppService.GetClassesAsync(parentId, studentId, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<List<ParentClassOverviewDto>>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<List<ParentClassOverviewDto>>(false, result.Message, null, result.Errors));
    }

    [HttpGet("{classId:int}")]
    public async Task<ActionResult<ApiResponse<ParentClassDetailDto>>> GetClassDetail(
        int classId,
        [FromQuery] int studentId,
        CancellationToken cancellationToken)
    {
        var parentId = GetUserId();
        if (parentId == 0) return Unauthorized();

        var result = await _parentAppService.GetClassDetailAsync(parentId, studentId, classId, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<ParentClassDetailDto>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<ParentClassDetailDto>(false, result.Message, null, result.Errors));
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
