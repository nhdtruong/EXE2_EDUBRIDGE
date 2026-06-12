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
[Route("api/v1/parent/invoices")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ParentOnly")]
public class ParentFinanceController : ControllerBase
{
    private readonly IParentAppService _parentAppService;

    public ParentFinanceController(IParentAppService parentAppService)
    {
        _parentAppService = parentAppService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ParentInvoiceDto>>>> GetInvoices(
        [FromQuery] int? studentId,
        CancellationToken cancellationToken)
    {
        var parentId = GetUserId();
        if (parentId == 0) return Unauthorized();

        var result = await _parentAppService.GetInvoicesAsync(parentId, studentId, cancellationToken);
        
        return result.IsSuccess
            ? Ok(new ApiResponse<List<ParentInvoiceDto>>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<List<ParentInvoiceDto>>(false, result.Message, null, result.Errors));
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
