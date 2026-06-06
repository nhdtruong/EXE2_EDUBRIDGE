using System.Security.Claims;
using EduBridge.Contracts.Classes;
using EduBridge.Data;
using EduBridge.Services.Classes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/classes")]
[Route("api/v1/classes")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class ClassesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IClassCreationService _classCreationService;
    private readonly IClassManagementService _classManagementService;
    private readonly IClassEnrollmentService _classEnrollmentService;

    public ClassesController(
        AppDbContext context,
        IClassCreationService classCreationService,
        IClassManagementService classManagementService,
        IClassEnrollmentService classEnrollmentService)
    {
        _context = context;
        _classCreationService = classCreationService;
        _classManagementService = classManagementService;
        _classEnrollmentService = classEnrollmentService;
    }

    [HttpGet("create-options")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "OWNER")]
    public async Task<ActionResult<ApiResponse<ClassCreateOptionsResponse>>> GetCreateOptionsAsync(
        [FromQuery] int centerId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized(new ApiResponse<ClassCreateOptionsResponse>(
                false, "Token không hợp lệ.", null));
        }

        var result = await _classCreationService.GetCreateOptionsAsync(userId.Value, centerId, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<ClassCreateOptionsResponse>(true, result.Message, result.Value))
            : ForbidOrBadRequest(result);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "OWNER")]
    public async Task<ActionResult<ApiResponse<ClassCreationResponse>>> CreateClassAsync(
        [FromBody] CreateClassRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized(new ApiResponse<ClassCreationResponse>(
                false, "Token không hợp lệ.", null));
        }

        var result = await _classCreationService.CreateAsync(userId.Value, request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(
                nameof(GetClassAsync),
                new { classId = result.Value!.ClassId },
                new ApiResponse<ClassCreationResponse>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<ClassCreationResponse>(
                false, result.Message, null, result.Errors));
    }

    [HttpPost("check-conflicts")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "OWNER")]
    public async Task<ActionResult<ApiResponse<ClassConflictResponse>>> CheckConflictsAsync(
        [FromBody] CreateClassRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized(new ApiResponse<ClassConflictResponse>(
                false, "Token không hợp lệ.", null));
        }

        var result = await _classCreationService.CheckConflictsAsync(userId.Value, request, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<ClassConflictResponse>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<ClassConflictResponse>(
                false, result.Message, null, result.Errors));
    }

    [HttpGet("{classId:int}/schedules")]
    public async Task<ActionResult<IReadOnlyList<ClassScheduleResponse>>> GetSchedulesAsync(
        int classId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var roleCode = User.FindFirstValue(ClaimTypes.Role);

        if (userId == null || string.IsNullOrWhiteSpace(roleCode))
        {
            return Unauthorized(new ApiErrorResponse("Token không hợp lệ."));
        }

        var classAccessible = await BuildAccessibleClassesQuery(userId.Value, roleCode)
            .AnyAsync(c => c.ClassId == classId, cancellationToken);

        if (!classAccessible)
        {
            return NotFound(new ApiErrorResponse("Không tìm thấy lớp học hoặc tài khoản không có quyền truy cập."));
        }

        var schedules = await _context.ClassSchedules
            .AsNoTracking()
            .Where(schedule => schedule.ClassId == classId)
            .OrderBy(schedule => schedule.DayOfWeek)
            .ThenBy(schedule => schedule.StartTime)
            .Select(schedule => new ClassScheduleResponse(
                schedule.ClassScheduleId,
                schedule.DayOfWeek,
                schedule.StartTime,
                schedule.EndTime))
            .ToListAsync(cancellationToken);

        return Ok(schedules);
    }

    [HttpPut("{classId:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "OWNER")]
    public async Task<ActionResult<ApiResponse<ClassMutationResponse>>> UpdateClassAsync(
        int classId,
        [FromBody] UpdateClassRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new ApiResponse<ClassMutationResponse>(false, "Token không hợp lệ.", null));
        request.ClassId = classId;
        var result = await _classManagementService.UpdateAsync(userId.Value, request, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<ClassMutationResponse>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<ClassMutationResponse>(false, result.Message, null, result.Errors));
    }

    [HttpGet("{classId:int}/edit")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "OWNER")]
    public async Task<ActionResult<ApiResponse<ClassEditResponse>>> GetClassForEditAsync(
        int classId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new ApiResponse<ClassEditResponse>(false, "Token không hợp lệ.", null));
        var result = await _classManagementService.GetEditAsync(userId.Value, classId, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<ClassEditResponse>(true, result.Message, result.Value))
            : NotFound(new ApiResponse<ClassEditResponse>(false, result.Message, null, result.Errors));
    }

    [HttpPost("{classId:int}/close")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "OWNER")]
    public async Task<ActionResult<ApiResponse<ClassMutationResponse>>> CloseClassAsync(int classId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new ApiResponse<ClassMutationResponse>(false, "Token không hợp lệ.", null));
        var result = await _classManagementService.CloseAsync(userId.Value, classId, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<ClassMutationResponse>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<ClassMutationResponse>(false, result.Message, null, result.Errors));
    }

    [HttpDelete("{classId:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "OWNER")]
    public async Task<ActionResult<ApiResponse<ClassMutationResponse>>> DeleteClassAsync(int classId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new ApiResponse<ClassMutationResponse>(false, "Token không hợp lệ.", null));
        var result = await _classManagementService.SoftDeleteAsync(userId.Value, classId, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<ClassMutationResponse>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<ClassMutationResponse>(false, result.Message, null, result.Errors));
    }

    [HttpGet("{classId:int}/students")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "OWNER")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EnrolledStudentResponse>>>> GetEnrolledStudentsAsync(int classId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new ApiResponse<IReadOnlyList<EnrolledStudentResponse>>(false, "Token không hợp lệ.", null));
        var result = await _classEnrollmentService.GetEnrolledStudentsAsync(userId.Value, classId, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<IReadOnlyList<EnrolledStudentResponse>>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<IReadOnlyList<EnrolledStudentResponse>>(false, result.Message, null, result.Errors));
    }

    [HttpGet("{classId:int}/students/available")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "OWNER")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AvailableStudentResponse>>>> GetAvailableStudentsAsync(int classId, [FromQuery] string? keyword, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new ApiResponse<IReadOnlyList<AvailableStudentResponse>>(false, "Token không hợp lệ.", null));
        var result = await _classEnrollmentService.GetAvailableStudentsAsync(userId.Value, classId, keyword, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<IReadOnlyList<AvailableStudentResponse>>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<IReadOnlyList<AvailableStudentResponse>>(false, result.Message, null, result.Errors));
    }

    [HttpPost("{classId:int}/students")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "OWNER")]
    public async Task<ActionResult<ApiResponse<EnrollStudentsResponse>>> EnrollStudentsAsync(int classId, [FromBody] EnrollStudentRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new ApiResponse<EnrollStudentsResponse>(false, "Token không hợp lệ.", null));
        var result = await _classEnrollmentService.EnrollStudentsAsync(userId.Value, classId, request, cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<EnrollStudentsResponse>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<EnrollStudentsResponse>(false, result.Message, null, result.Errors));
    }

    [HttpDelete("{classId:int}/students/{studentId:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "OWNER")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveStudentAsync(int classId, int studentId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new ApiResponse<bool>(false, "Token không hợp lệ.", false));
        var result = await _classEnrollmentService.RemoveStudentAsync(
            userId.Value,
            classId,
            studentId,
            cancellationToken: cancellationToken);
        return result.IsSuccess
            ? Ok(new ApiResponse<bool>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<bool>(false, result.Message, false, result.Errors));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApiClassListItem>>> GetClassesAsync(
        [FromQuery] int? centerId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var roleCode = User.FindFirstValue(ClaimTypes.Role);

        if (userId == null || string.IsNullOrWhiteSpace(roleCode))
        {
            return Unauthorized(new ApiErrorResponse("Token không hợp lệ."));
        }

        var query = BuildAccessibleClassesQuery(userId.Value, roleCode);

        if (centerId is > 0)
        {
            query = query.Where(c => c.CenterId == centerId.Value);
        }

        var classes = await query
            .OrderByDescending(c => c.StartDate)
            .ThenByDescending(c => c.ClassId)
            .Select(c => new ApiClassListItem(
                c.ClassId,
                c.CenterId,
                c.ClassCode,
                c.ClassName,
                c.Course.CourseName,
                c.Teacher.User.FullName,
                c.RoomNavigation != null ? c.RoomNavigation.RoomName : c.Room,
                c.StartDate,
                c.EndDate,
                c.TotalSessions,
                c.Status,
                c.ClassSchedules
                    .OrderBy(s => s.DayOfWeek)
                    .ThenBy(s => s.StartTime)
                    .Select(s => new ApiClassScheduleItem(s.DayOfWeek, s.StartTime, s.EndTime))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return Ok(classes);
    }

    [HttpGet("{classId:int}")]
    public async Task<ActionResult<ApiClassListItem>> GetClassAsync(
        int classId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var roleCode = User.FindFirstValue(ClaimTypes.Role);

        if (userId == null || string.IsNullOrWhiteSpace(roleCode))
        {
            return Unauthorized(new ApiErrorResponse("Token không hợp lệ."));
        }

        var item = await BuildAccessibleClassesQuery(userId.Value, roleCode)
            .Where(c => c.ClassId == classId)
            .Select(c => new ApiClassListItem(
                c.ClassId,
                c.CenterId,
                c.ClassCode,
                c.ClassName,
                c.Course.CourseName,
                c.Teacher.User.FullName,
                c.RoomNavigation != null ? c.RoomNavigation.RoomName : c.Room,
                c.StartDate,
                c.EndDate,
                c.TotalSessions,
                c.Status,
                c.ClassSchedules
                    .OrderBy(s => s.DayOfWeek)
                    .ThenBy(s => s.StartTime)
                    .Select(s => new ApiClassScheduleItem(s.DayOfWeek, s.StartTime, s.EndTime))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return item == null
            ? NotFound(new ApiErrorResponse("Không tìm thấy lớp học hoặc tài khoản không có quyền truy cập."))
            : Ok(item);
    }

    private IQueryable<Models.Class> BuildAccessibleClassesQuery(int userId, string roleCode)
    {
        var query = _context.Classes
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.Status != "Closed");

        return roleCode.ToUpperInvariant() switch
        {
            "OWNER" => query.Where(c =>
                c.Center.OwnerUserId == userId ||
                _context.CenterUsers.Any(cu =>
                    cu.CenterId == c.CenterId &&
                    cu.UserId == userId &&
                    cu.UserType == "OWNER" &&
                    cu.Status == "Active")),
            "TEACHER" => query.Where(c =>
                c.Teacher.UserId == userId &&
                !c.Teacher.IsDeleted &&
                c.Teacher.Status == "Active"),
            "PARENT" => query.Where(c =>
                c.Enrollments.Any(e =>
                    e.Status == "Đang học" &&
                    !e.Student.IsDeleted &&
                    e.Student.ParentUserId == userId)),
            _ => query.Where(_ => false)
        };
    }

    private int? GetCurrentUserId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;
    }

    private ActionResult<ApiResponse<ClassCreateOptionsResponse>> ForbidOrBadRequest(
        ClassOperationResult<ClassCreateOptionsResponse> result)
    {
        if (result.Errors.ContainsKey("CenterId"))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new ApiResponse<ClassCreateOptionsResponse>(
                    false, result.Message, null, result.Errors));
        }

        return BadRequest(new ApiResponse<ClassCreateOptionsResponse>(
            false, result.Message, null, result.Errors));
    }
}

public sealed record ApiClassListItem(
    int ClassId,
    int CenterId,
    string ClassCode,
    string ClassName,
    string CourseName,
    string TeacherName,
    string? RoomName,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalSessions,
    string Status,
    IReadOnlyList<ApiClassScheduleItem> Schedules);

public sealed record ApiClassScheduleItem(
    byte DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);
