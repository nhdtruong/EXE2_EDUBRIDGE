using System.Security.Claims;
using EduBridge.Contracts.Finance;
using EduBridge.Services.Finance;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduBridge.Controllers.Api;

public sealed record CancelPaymentRequest(string Reason);

[ApiController]
[Route("api/v1/owner/payments")]
[Authorize(Roles = "OWNER")]
public sealed class OwnerPaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public OwnerPaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    private int? GetCurrentUserId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
    }

    [HttpPost]
    public async Task<ActionResult> CreatePaymentAsync([FromQuery] int centerId, [FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _paymentService.CreatePaymentAsync(request, centerId, userId.Value, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });
        return Ok(new { success = true, message = result.Message, data = result.Value });
    }

    [HttpGet]
    public async Task<ActionResult> GetListAsync([FromQuery] int centerId, [FromQuery] PaymentFilterRequest filter, CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetListAsync(centerId, filter, cancellationToken);
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

    [HttpPost("{paymentId}/cancel")]
    public async Task<ActionResult> CancelPaymentAsync([FromQuery] int centerId, int paymentId, [FromBody] CancelPaymentRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _paymentService.CancelPaymentAsync(paymentId, centerId, userId.Value, request.Reason, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });
        return Ok(new { success = true, message = result.Message });
    }
}
