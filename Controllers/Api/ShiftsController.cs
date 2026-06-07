using System.Security.Claims;
using EduBridge.Contracts.Classes;
using EduBridge.Contracts.Shifts;
using EduBridge.Services.Classes;
using EduBridge.Services.Shifts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/v1/shifts")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "OWNER")]
public sealed class ShiftsController : ControllerBase
{
    private readonly IShiftManagementService _service;

    public ShiftsController(IShiftManagementService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ShiftPagedResponse>>> GetShifts(
        [FromQuery] ShiftQuery query, CancellationToken cancellationToken) =>
        ToActionResult(await _service.GetShiftsAsync(GetUserId(), query, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ShiftMutationResponse>>> Create(
        [FromBody] SaveShiftRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await _service.CreateAsync(GetUserId(), request, cancellationToken));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ShiftMutationResponse>>> Update(
        int id, [FromBody] SaveShiftRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await _service.UpdateAsync(GetUserId(), id, request, cancellationToken));

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<ApiResponse<ShiftMutationResponse>>> UpdateStatus(
        int id, [FromBody] ShiftStatusRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await _service.SetStatusAsync(GetUserId(), id, request.Status, cancellationToken));

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        int id, CancellationToken cancellationToken) =>
        ToActionResult(await _service.DeleteShiftAsync(GetUserId(), id, cancellationToken));

    private int GetUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private ActionResult<ApiResponse<T>> ToActionResult<T>(ClassOperationResult<T> result) =>
        result.IsSuccess
            ? Ok(new ApiResponse<T>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<T>(false, result.Message, default, result.Errors));
}
