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
[Route("api/v1/parent/children/{studentId:int}")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ParentOnly")]
public class ParentAcademicController : ControllerBase
{
    private readonly IParentAppService _parentAppService;

    public ParentAcademicController(IParentAppService parentAppService)
    {
        _parentAppService = parentAppService;
    }

    [HttpGet("grades")]
    public async Task<ActionResult<ApiResponse<List<ParentGradeDto>>>> GetGrades(int studentId, CancellationToken cancellationToken)
    {
        var parentId = GetUserId();
        if (parentId == 0) return Unauthorized();

        var result = await _parentAppService.GetGradesAsync(parentId, studentId, cancellationToken);
        
        return result.IsSuccess
            ? Ok(new ApiResponse<List<ParentGradeDto>>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<List<ParentGradeDto>>(false, result.Message, null, result.Errors));
    }

    [HttpGet("homeworks")]
    public async Task<ActionResult<ApiResponse<List<ParentHomeworkDto>>>> GetHomeworks(int studentId, CancellationToken cancellationToken)
    {
        var parentId = GetUserId();
        if (parentId == 0) return Unauthorized();

        var result = await _parentAppService.GetHomeworksAsync(parentId, studentId, cancellationToken);
        
        return result.IsSuccess
            ? Ok(new ApiResponse<List<ParentHomeworkDto>>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<List<ParentHomeworkDto>>(false, result.Message, null, result.Errors));
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
