using System.Security.Claims;
using EduBridge.Contracts.Finance;
using EduBridge.Data;
using EduBridge.Services.Finance;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/v1/owner/invoices")]
[Authorize(Roles = "OWNER")]
public sealed class OwnerInvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly AppDbContext _context;

    public OwnerInvoicesController(IInvoiceService invoiceService, AppDbContext context)
    {
        _invoiceService = invoiceService;
        _context = context;
    }

    private int? GetCurrentUserId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
    }

    private async Task<bool> VerifyCenterOwnershipAsync(int centerId, int userId)
    {
        return await _context.Centers.AnyAsync(c => c.CenterId == centerId && c.OwnerUserId == userId && c.Status == "Active") || 
               await _context.CenterUsers.AnyAsync(cu => cu.CenterId == centerId && cu.UserId == userId && cu.UserType == "OWNER" && cu.Status == "Active");
    }

    [HttpPost]
    public async Task<ActionResult> CreateInvoiceAsync([FromQuery] int centerId, [FromBody] CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (!await VerifyCenterOwnershipAsync(centerId, userId.Value)) return Forbid();

        var result = await _invoiceService.CreateInvoiceAsync(request, centerId, userId.Value, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });
        return Ok(new { success = true, message = result.Message, data = result.Value });
    }

    [HttpGet("{invoiceId}")]
    public async Task<ActionResult> GetByIdAsync([FromQuery] int centerId, int invoiceId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (!await VerifyCenterOwnershipAsync(centerId, userId.Value)) return Forbid();

        var result = await _invoiceService.GetByIdAsync(invoiceId, centerId, cancellationToken);
        if (!result.IsSuccess) return NotFound(new { success = false, message = result.Message });
        return Ok(new { success = true, data = result.Value });
    }

    [HttpGet]
    public async Task<ActionResult> GetListAsync([FromQuery] int centerId, [FromQuery] InvoiceFilterRequest filter, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (!await VerifyCenterOwnershipAsync(centerId, userId.Value)) return Forbid();

        var result = await _invoiceService.GetListAsync(centerId, filter, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });
        
        var paged = result.Value!;
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
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (!await VerifyCenterOwnershipAsync(centerId, userId.Value)) return Forbid();

        var result = await _invoiceService.GetDebtListAsync(centerId, filter, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });
        return Ok(new { success = true, data = result.Value });
    }
}
