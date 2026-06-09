using System.Security.Claims;
using EduBridge.Contracts.Finance;
using EduBridge.Data;
using EduBridge.Services.Finance;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/v1/owner/invoices")]
[Authorize(Roles = "OWNER")]
public sealed class OwnerInvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public OwnerInvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    private int? GetCurrentUserId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
    }

    [HttpPost]
    public async Task<ActionResult> CreateInvoiceAsync([FromQuery] int centerId, [FromBody] CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _invoiceService.CreateInvoiceAsync(request, centerId, userId.Value, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });
        return Ok(new { success = true, message = result.Message, data = result.Value });
    }

    [HttpGet("{invoiceId}")]
    public async Task<ActionResult> GetByIdAsync([FromQuery] int centerId, int invoiceId, CancellationToken cancellationToken)
    {
        var result = await _invoiceService.GetByIdAsync(invoiceId, centerId, cancellationToken);
        if (!result.IsSuccess) return NotFound(new { success = false, message = result.Message });
        return Ok(new { success = true, data = result.Value });
    }

    [HttpGet]
    public async Task<ActionResult> GetListAsync([FromQuery] int centerId, [FromQuery] InvoiceFilterRequest filter, CancellationToken cancellationToken)
    {
        var result = await _invoiceService.GetListAsync(centerId, filter, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });
        
        var paged = result.Value;
        return Ok(new {
            success = true,
            data = paged.Items,
            pagination = new {
                page = paged.PageNumber,
                pageSize = paged.PageSize,
                totalItems = paged.TotalItems,
                totalPages = paged.TotalPages
            }
        });
    }

    [HttpGet("student-debts")]
    public async Task<ActionResult> GetDebtListAsync([FromQuery] int centerId, [FromQuery] DebtFilterRequest filter, CancellationToken cancellationToken)
    {
        var result = await _invoiceService.GetDebtListAsync(centerId, filter, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });
        return Ok(new { success = true, data = result.Value });
    }
}
