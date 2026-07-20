using EduBridge.Contracts.Classes;
using EduBridge.Contracts.Courses;
using EduBridge.Services.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/v1/courses")]
[Authorize(Policy = "AdminOnly")]
public class CoursesController : ControllerBase
{
    private readonly ICourseManagementService _service;

    public CoursesController(ICourseManagementService service)
    {
        _service = service;
    }

    private int GetOwnerUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet]
    public async Task<IActionResult> GetCourses([FromQuery] CourseQuery query, CancellationToken cancellationToken)
    {
        var result = await _service.GetCoursesAsync(GetOwnerUserId(), query, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });
        
        return Ok(new
        {
            success = true,
            data = result.Value!.Data,
            pagination = new
            {
                page = result.Value.Page,
                pageSize = result.Value.PageSize,
                totalItems = result.Value.TotalItems,
                totalPages = result.Value.TotalPages
            }
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCourse(int id, CancellationToken cancellationToken)
    {
        var result = await _service.GetCourseAsync(GetOwnerUserId(), id, cancellationToken);
        if (!result.IsSuccess) return NotFound(new { success = false, message = result.Message });
        
        return Ok(new { success = true, data = result.Value });
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourse([FromBody] SaveCourseRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Validation failed" });

        var result = await _service.CreateCourseAsync(GetOwnerUserId(), request, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = result.Message, data = result.Value });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] SaveCourseRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Validation failed" });

        var result = await _service.UpdateCourseAsync(GetOwnerUserId(), id, request, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = result.Message, data = result.Value });
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] UpdateCourseStatusRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(new { success = false, message = "Validation failed" });

        var result = await _service.SetStatusAsync(GetOwnerUserId(), id, request.Status, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = result.Message, data = result.Value });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCourse(int id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteCourseAsync(GetOwnerUserId(), id, cancellationToken);
        if (!result.IsSuccess) return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = result.Message });
    }
}
