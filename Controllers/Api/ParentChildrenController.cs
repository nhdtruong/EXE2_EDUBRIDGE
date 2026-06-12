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
[Route("api/v1/parent/children")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ParentOnly")]
public class ParentChildrenController : ControllerBase
{
    private readonly IParentAppService _parentAppService;

    public ParentChildrenController(IParentAppService parentAppService)
    {
        _parentAppService = parentAppService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ParentChildOverviewDto>>>> GetChildren(CancellationToken cancellationToken)
    {
        var parentId = GetUserId();
        if (parentId == 0) return Unauthorized();

        var result = await _parentAppService.GetChildrenAsync(parentId, cancellationToken);
        
        return result.IsSuccess
            ? Ok(new ApiResponse<List<ParentChildOverviewDto>>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<List<ParentChildOverviewDto>>(false, result.Message, null, result.Errors));
    }

    [HttpGet("{studentId:int}")]
    public async Task<ActionResult<ApiResponse<ParentChildDetailDto>>> GetChildDetail(int studentId, CancellationToken cancellationToken)
    {
        var parentId = GetUserId();
        if (parentId == 0) return Unauthorized();

        var result = await _parentAppService.GetChildDetailAsync(parentId, studentId, cancellationToken);
        
        return result.IsSuccess
            ? Ok(new ApiResponse<ParentChildDetailDto>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<ParentChildDetailDto>(false, result.Message, null, result.Errors));
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
