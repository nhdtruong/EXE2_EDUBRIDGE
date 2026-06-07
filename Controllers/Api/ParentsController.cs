using System.Security.Claims;
using EduBridge.Contracts.Classes;
using EduBridge.Contracts.Parents;
using EduBridge.Services.Parents;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/v1/parents")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "OWNER")]
public sealed class ParentsController : ControllerBase
{
    private readonly IParentManagementService _service;

    public ParentsController(IParentManagementService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ParentPagedResponse>>> GetAsync(
        [FromQuery] ParentQuery query, CancellationToken cancellationToken) =>
        ToActionResult(await _service.GetParentsAsync(GetUserId(), query, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ParentDetailResponse>>> GetByIdAsync(
        int id, CancellationToken cancellationToken) =>
        ToActionResult(await _service.GetParentAsync(GetUserId(), id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ParentMutationResponse>>> CreateAsync(
        [FromBody] SaveParentRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await _service.CreateAsync(GetUserId(), request, cancellationToken));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ParentMutationResponse>>> UpdateAsync(
        int id, [FromBody] SaveParentRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await _service.UpdateAsync(GetUserId(), id, request, cancellationToken));

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<ApiResponse<ParentMutationResponse>>> SetStatusAsync(
        int id, [FromBody] ParentStatusRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await _service.SetStatusAsync(GetUserId(), id, request.Status, cancellationToken));

    [HttpPost("{id:int}/children/{studentId:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> LinkStudentAsync(
        int id, int studentId, CancellationToken cancellationToken) =>
        ToActionResult(await _service.LinkStudentAsync(GetUserId(), id, studentId, cancellationToken));

    [HttpGet("{id:int}/children/linkable")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LinkableStudentResponse>>>> GetLinkableStudentsAsync(
        int id, [FromQuery] string? keyword, CancellationToken cancellationToken) =>
        ToActionResult(await _service.GetLinkableStudentsAsync(GetUserId(), id, keyword, cancellationToken));

    [HttpPost("{id:int}/reset-password")]
    public async Task<ActionResult<ApiResponse<ResetParentPasswordResponse>>> ResetPasswordAsync(
        int id, CancellationToken cancellationToken) =>
        ToActionResult(await _service.ResetPasswordAsync(GetUserId(), id, cancellationToken));

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private ActionResult<ApiResponse<T>> ToActionResult<T>(Services.Classes.ClassOperationResult<T> result) =>
        result.IsSuccess
            ? Ok(new ApiResponse<T>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<T>(false, result.Message, default, result.Errors));
}

public sealed record ParentStatusRequest(string Status);
