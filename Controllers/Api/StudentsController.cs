using System.Security.Claims;
using EduBridge.Contracts.Students;
using EduBridge.Services.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/v1/students")]
[Authorize(Policy = "AdminOnly")]
public class StudentsController : ControllerBase
{
    private readonly IStudentManagementService _studentService;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(IStudentManagementService studentService, ILogger<StudentsController> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    [HttpGet]
    public async Task<IActionResult> GetStudents([FromQuery] StudentQuery query, CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return Unauthorized(new { success = false, message = "Unauthorized" });

        var result = await _studentService.GetStudentsAsync(ownerUserId.Value, query, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });

        return Ok(new
        {
            success = true,
            data = result.Value?.Data,
            pagination = new
            {
                page = result.Value?.Page,
                pageSize = result.Value?.PageSize,
                totalItems = result.Value?.TotalItems,
                totalPages = result.Value?.TotalPages
            }
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetStudent(int id, CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return Unauthorized(new { success = false, message = "Unauthorized" });

        var result = await _studentService.GetStudentAsync(ownerUserId.Value, id, cancellationToken);
        if (!result.IsSuccess) return NotFound(new { success = false, message = result.Message });

        return Ok(new { success = true, data = result.Value });
    }

    [HttpGet("parents/search")]
    public async Task<IActionResult> SearchParents([FromQuery] string keyword, CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return Unauthorized(new { success = false, message = "Unauthorized" });

        var result = await _studentService.SearchParentsAsync(ownerUserId.Value, keyword, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, found = result.Value?.Count > 0, parents = result.Value });
    }

    [HttpPost]
    public async Task<IActionResult> CreateStudent([FromForm] SaveStudentRequest request, CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return Unauthorized(new { success = false, message = "Unauthorized" });

        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Validation failed", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        var result = await _studentService.CreateStudentAsync(ownerUserId.Value, request, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = "Thêm học sinh thành công", data = result.Value });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStudent(int id, [FromForm] UpdateStudentRequest request, CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return Unauthorized(new { success = false, message = "Unauthorized" });

        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Validation failed", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        var result = await _studentService.UpdateStudentAsync(ownerUserId.Value, id, request, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = "Cập nhật học sinh thành công", data = result.Value });
    }

    [HttpPatch("{id}/parent")]
    public async Task<IActionResult> UpdateStudentParent(int id, [FromBody] UpdateStudentParentRequest request, CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return Unauthorized(new { success = false, message = "Unauthorized" });

        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Validation failed", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        var result = await _studentService.UpdateStudentParentAsync(ownerUserId.Value, id, request, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = "Cập nhật phụ huynh thành công", data = result.Value });
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return Unauthorized(new { success = false, message = "Unauthorized" });

        var result = await _studentService.ToggleStudentStatusAsync(ownerUserId.Value, id, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = "Thay đổi trạng thái thành công", data = result.Value });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStudent(int id, CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return Unauthorized(new { success = false, message = "Unauthorized" });

        var result = await _studentService.DeleteStudentAsync(ownerUserId.Value, id, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = "Xóa học sinh thành công" });
    }
}
