using System.Security.Claims;
using EduBridge.Contracts.Finance;
using EduBridge.Data;
using EduBridge.Services.Finance;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Controllers.Api;

public sealed record VoidReceiptRequest(string Reason);

[ApiController]
[Route("api/v1/owner/receipts")]
[Authorize(Roles = "OWNER")]
public sealed class OwnerReceiptsController : ControllerBase
{
    private readonly IReceiptService _receiptService;
    private readonly AppDbContext _context;

    public OwnerReceiptsController(IReceiptService receiptService, AppDbContext context)
    {
        _receiptService = receiptService;
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
    public async Task<ActionResult> IssueReceiptAsync([FromQuery] int centerId, [FromBody] IssueReceiptRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (!await VerifyCenterOwnershipAsync(centerId, userId.Value)) return Forbid();

        var result = await _receiptService.IssueReceiptAsync(request, centerId, userId.Value, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });
        return Ok(new { success = true, message = result.Message, data = result.Value });
    }

    [HttpGet("{receiptId}")]
    public async Task<ActionResult> GetByIdAsync([FromQuery] int centerId, int receiptId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (!await VerifyCenterOwnershipAsync(centerId, userId.Value)) return Forbid();

        var result = await _receiptService.GetByIdAsync(receiptId, centerId, cancellationToken);
        if (!result.IsSuccess) return NotFound(new { success = false, message = result.Message });
        return Ok(new { success = true, data = result.Value });
    }

    [HttpGet]
    public async Task<ActionResult> GetListAsync([FromQuery] int centerId, [FromQuery] ReceiptFilterRequest filter, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (!await VerifyCenterOwnershipAsync(centerId, userId.Value)) return Forbid();

        var result = await _receiptService.GetListAsync(centerId, filter, cancellationToken);
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

    [HttpPost("{receiptId}/void")]
    public async Task<ActionResult> VoidReceiptAsync([FromQuery] int centerId, int receiptId, [FromBody] VoidReceiptRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (!await VerifyCenterOwnershipAsync(centerId, userId.Value)) return Forbid();

        var result = await _receiptService.VoidReceiptAsync(receiptId, centerId, userId.Value, request.Reason, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });
        return Ok(new { success = true, message = result.Message });
    }
}
