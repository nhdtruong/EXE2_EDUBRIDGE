using System.Security.Claims;
using EduBridge.Contracts.Classes;
using EduBridge.Contracts.Rooms;
using EduBridge.Services.Classes;
using EduBridge.Services.Rooms;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/v1/rooms")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "OWNER")]
public sealed class RoomsController : ControllerBase
{
    private readonly IRoomManagementService _service;

    public RoomsController(IRoomManagementService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<RoomPagedResponse>>> GetRooms(
        [FromQuery] RoomQuery query, CancellationToken cancellationToken) =>
        ToActionResult(await _service.GetRoomsAsync(GetUserId(), query, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoomMutationResponse>>> Create(
        [FromBody] CreateRoomRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await _service.CreateAsync(GetUserId(), request, cancellationToken));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<RoomMutationResponse>>> Update(
        int id, [FromBody] UpdateRoomRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await _service.UpdateAsync(GetUserId(), id, request, cancellationToken));

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<ApiResponse<RoomMutationResponse>>> UpdateStatus(
        int id, [FromBody] RoomStatusRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await _service.SetStatusAsync(GetUserId(), id, request.Status, cancellationToken));

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        int id, CancellationToken cancellationToken) =>
        ToActionResult(await _service.DeleteRoomAsync(GetUserId(), id, cancellationToken));

    private int GetUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private ActionResult<ApiResponse<T>> ToActionResult<T>(ClassOperationResult<T> result) =>
        result.IsSuccess
            ? Ok(new ApiResponse<T>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<T>(false, result.Message, default, result.Errors));
}
