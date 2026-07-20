using System.Security.Claims;
using EduBridge.Contracts.Classes;
using EduBridge.Data;
using EduBridge.Models.DTOs.ParentApp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/v1/parent/children/{studentId:int}/leave-requests")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ParentOnly")]
public sealed class ParentLeaveRequestsController : ControllerBase
{
    private readonly AppDbContext _context;
    public ParentLeaveRequestsController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ParentLeaveRequestDto>>>> Get(int studentId, CancellationToken cancellationToken)
    {
        var parentId = GetUserId();
        if (!await OwnsStudent(parentId, studentId, cancellationToken)) return Forbid();
        var items = await _context.Database.SqlQuery<ParentLeaveRequestDto>($"""
            SELECT lr.LeaveRequestId, lr.StudentId, lr.LessonId, l.LessonTitle, l.LessonDate,
                   lr.Reason, lr.Status, lr.ReviewNote, lr.CreatedAt
            FROM LeaveRequests lr JOIN Lessons l ON l.LessonId = lr.LessonId
            WHERE lr.StudentId = {studentId}
            ORDER BY lr.CreatedAt DESC
            """).ToListAsync(cancellationToken);
        return Ok(new ApiResponse<List<ParentLeaveRequestDto>>(true, "Thành công", items));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> Create(int studentId, CreateParentLeaveRequest request, CancellationToken cancellationToken)
    {
        var parentId = GetUserId();
        if (!await OwnsStudent(parentId, studentId, cancellationToken)) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(new ApiResponse<bool>(false, "Vui lòng nhập lý do.", false));
        var lessonId = await _context.Enrollments
            .Where(e => e.StudentId == studentId)
            .SelectMany(e => e.Class.Lessons)
            .Where(l => (request.LessonId.HasValue && l.LessonId == request.LessonId) ||
                        (!request.LessonId.HasValue && request.LessonDate.HasValue && l.LessonDate == request.LessonDate))
            .OrderBy(l => l.StartTime)
            .Select(l => (int?)l.LessonId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!lessonId.HasValue) return BadRequest(new ApiResponse<bool>(false, "Không tìm thấy buổi học phù hợp.", false));
        await _context.Database.ExecuteSqlInterpolatedAsync($"""
            IF NOT EXISTS (SELECT 1 FROM LeaveRequests WHERE StudentId={studentId} AND LessonId={lessonId.Value} AND Status='Pending')
            INSERT INTO LeaveRequests(StudentId, LessonId, ParentUserId, Reason, Status, CreatedAt)
            VALUES({studentId}, {lessonId.Value}, {parentId}, {request.Reason.Trim()}, 'Pending', SYSDATETIME())
            """, cancellationToken);
        return Ok(new ApiResponse<bool>(true, "Đã gửi yêu cầu xin nghỉ.", true));
    }

    private int GetUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private Task<bool> OwnsStudent(int parentId, int studentId, CancellationToken ct) =>
        _context.Students.AnyAsync(s => s.StudentId == studentId && s.ParentUserId == parentId && !s.IsDeleted, ct);
}
