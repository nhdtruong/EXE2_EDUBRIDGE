using System.Security.Claims;
using EduBridge.Contracts.Classes;
using EduBridge.Contracts.Teachers;
using EduBridge.Services.Teachers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/v1/teachers")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "OWNER")]
public sealed class TeachersController : ControllerBase
{
    private readonly ITeacherManagementService _service;

    public TeachersController(ITeacherManagementService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<TeacherPagedResponse>>> GetTeachers(
        [FromQuery] TeacherQuery query, CancellationToken cancellationToken) =>
        ToActionResult(await _service.GetTeachersAsync(GetUserId(), query, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<TeacherDetailResponse>>> GetTeacher(
        int id, CancellationToken cancellationToken) =>
        ToActionResult(await _service.GetTeacherAsync(GetUserId(), id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TeacherMutationResponse>>> Create(
        [FromBody] SaveTeacherRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await _service.CreateAsync(GetUserId(), request, cancellationToken));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<TeacherMutationResponse>>> Update(
        int id, [FromBody] SaveTeacherRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await _service.UpdateAsync(GetUserId(), id, request, cancellationToken));

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<ApiResponse<TeacherMutationResponse>>> UpdateStatus(
        int id, [FromBody] TeacherStatusRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await _service.SetStatusAsync(GetUserId(), id, request.Status, cancellationToken));

    [HttpPost("{id:int}/reset-password")]
    public async Task<ActionResult<ApiResponse<ResetTeacherPasswordResponse>>> ResetPassword(
        int id, CancellationToken cancellationToken) =>
        ToActionResult(await _service.ResetPasswordAsync(GetUserId(), id, cancellationToken));

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        int id, CancellationToken cancellationToken) =>
        ToActionResult(await _service.DeleteTeacherAsync(GetUserId(), id, cancellationToken));

    [HttpPost("{id:int}/avatar")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<string?>>> UploadAvatar(
        int id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0) return BadRequest(new ApiResponse<string?>(false, "File trống.", null, null));
        if (file.Length > 2 * 1024 * 1024) return BadRequest(new ApiResponse<string?>(false, "File tối đa 2MB.", null, null));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            return BadRequest(new ApiResponse<string?>(false, "Chỉ hỗ trợ JPG, PNG, WEBP.", null, null));

        await using var stream = file.OpenReadStream();
        return ToActionResult(await _service.UpdateAvatarAsync(GetUserId(), id, stream, file.FileName, file.ContentType, cancellationToken));
    }

    [HttpDelete("{id:int}/avatar")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveAvatar(
        int id, CancellationToken cancellationToken) =>
        ToActionResult(await _service.RemoveAvatarAsync(GetUserId(), id, cancellationToken));

    private int GetUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private ActionResult<ApiResponse<T>> ToActionResult<T>(Services.Classes.ClassOperationResult<T> result) =>
        result.IsSuccess
            ? Ok(new ApiResponse<T>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<T>(false, result.Message, default, result.Errors));
}

public sealed record TeacherStatusRequest(string Status);
