using System.Security.Claims;
using EduBridge.Contracts.Finance;
using EduBridge.Data;
using EduBridge.Services.Finance;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Controllers.Api;

public sealed record CancelPaymentRequest(string Reason);

[ApiController]
[Route("api/v1/owner/payments")]
[Authorize(Roles = "OWNER")]
public sealed class OwnerPaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly AppDbContext _context;

    public OwnerPaymentsController(IPaymentService paymentService, AppDbContext context)
    {
        _paymentService = paymentService;
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
    public async Task<ActionResult> CreatePaymentAsync([FromQuery] int centerId, [FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (!await VerifyCenterOwnershipAsync(centerId, userId.Value)) return Forbid();

        var result = await _paymentService.CreatePaymentAsync(request, centerId, userId.Value, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });
        return Ok(new { success = true, message = result.Message, data = result.Value });
    }

    [HttpGet]
    public async Task<ActionResult> GetListAsync([FromQuery] int centerId, [FromQuery] PaymentFilterRequest filter, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (!await VerifyCenterOwnershipAsync(centerId, userId.Value)) return Forbid();

        var result = await _paymentService.GetListAsync(centerId, filter, cancellationToken);
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

    [HttpPost("{paymentId}/cancel")]
    public async Task<ActionResult> CancelPaymentAsync([FromQuery] int centerId, int paymentId, [FromBody] CancelPaymentRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (!await VerifyCenterOwnershipAsync(centerId, userId.Value)) return Forbid();

        var result = await _paymentService.CancelPaymentAsync(paymentId, centerId, userId.Value, request.Reason, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });
        return Ok(new { success = true, message = result.Message });
    }
}
