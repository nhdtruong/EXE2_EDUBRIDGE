using System.Security.Claims;
using EduBridge.Contracts.Classes;
using EduBridge.Services.Classes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages.AdminClasses;

[Authorize(Policy = "AdminOnly")]
public sealed class EditModel : PageModel
{
    private readonly IClassManagementService _classManagementService;
    private readonly IClassEnrollmentService _classEnrollmentService;

    public EditModel(
        IClassManagementService classManagementService,
        IClassEnrollmentService classEnrollmentService)
    {
        _classManagementService = classManagementService;
        _classEnrollmentService = classEnrollmentService;
    }

    [BindProperty]
    public UpdateClassRequest Input { get; set; } = new();

    public string ClassCode { get; private set; } = string.Empty;

    public ClassCreateOptionsResponse Options { get; private set; } =
        new(string.Empty, [], [], [], []);

    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "general";

    public IReadOnlyList<EnrolledStudentResponse> EnrolledStudents { get; private set; } = [];

    public bool IsStudentsTab => string.Equals(Tab, "students", StringComparison.OrdinalIgnoreCase);

    public async Task<IActionResult> OnGetAsync(int classId, CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return RedirectToPage("/Login");

        var result = await _classManagementService.GetEditAsync(ownerUserId.Value, classId, cancellationToken);
        if (!result.IsSuccess || result.Value == null)
        {
            TempData["ToastError"] = result.Message;
            return RedirectToPage("/AdminClasses");
        }

        Map(result.Value);
        await LoadStudentsAsync(ownerUserId.Value, classId, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return RedirectToPage("/Login");

        var result = await _classManagementService.UpdateAsync(ownerUserId.Value, Input, cancellationToken);
        if (result.IsSuccess)
        {
            TempData["ToastMessage"] = result.Message;
            return RedirectToPage("/AdminClasses");
        }

        foreach (var error in result.Errors)
        foreach (var message in error.Value)
            ModelState.AddModelError(string.IsNullOrWhiteSpace(error.Key) ? string.Empty : $"Input.{error.Key}", message);

        ModelState.AddModelError(string.Empty, result.Message);
        var loadResult = await _classManagementService.GetEditAsync(ownerUserId.Value, Input.ClassId, cancellationToken);
        if (loadResult.Value != null)
        {
            ClassCode = loadResult.Value.ClassCode;
            Options = loadResult.Value.Options;
        }
        return Page();
    }

    public async Task<IActionResult> OnGetAvailableStudentsAsync(
        int classId,
        string? keyword,
        CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return Unauthorized();
        var result = await _classEnrollmentService.GetAvailableStudentsAsync(ownerUserId.Value, classId, keyword, cancellationToken);
        return new JsonResult(new ApiResponse<IReadOnlyList<AvailableStudentResponse>>(
            result.IsSuccess, result.Message, result.Value, result.Errors))
        {
            StatusCode = result.IsSuccess ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest
        };
    }

    public async Task<IActionResult> OnPostEnrollStudentsAsync(
        int classId,
        CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return Unauthorized();
        var request = await Request.ReadFromJsonAsync<EnrollStudentRequest>(cancellationToken);
        if (request == null)
        {
            return BadRequest(new ApiResponse<EnrollStudentsResponse>(
                false,
                "Dữ liệu yêu cầu không hợp lệ.",
                null,
                new Dictionary<string, string[]> { ["StudentIds"] = ["Vui lòng chọn học sinh."] }));
        }
        var result = await _classEnrollmentService.EnrollStudentsAsync(ownerUserId.Value, classId, request, cancellationToken);
        return new JsonResult(new ApiResponse<EnrollStudentsResponse>(result.IsSuccess, result.Message, result.Value, result.Errors))
        {
            StatusCode = result.IsSuccess ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest
        };
    }

    public async Task<IActionResult> OnPostRemoveStudentAsync(
        int classId,
        int studentId,
        CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return Unauthorized();
        var result = await _classEnrollmentService.RemoveStudentAsync(ownerUserId.Value, classId, studentId, cancellationToken: cancellationToken);
        
        if (result.IsSuccess)
        {
            if (result.Message.Contains("LƯU Ý"))
            {
                TempData["ToastTitle"] = "Cần lưu ý";
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "warning";
            }
            else
            {
                TempData["ToastTitle"] = "Thành công";
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "success";
            }
        }
        
        return new JsonResult(new ApiResponse<bool>(result.IsSuccess, result.Message, result.Value, result.Errors))
        {
            StatusCode = result.IsSuccess ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest
        };
    }

    private void Map(ClassEditResponse value)
    {
        ClassCode = value.ClassCode;
        Options = value.Options;
        Input = new UpdateClassRequest
        {
            ClassId = value.ClassId,
            CenterId = value.CenterId,
            ClassName = value.ClassName,
            CourseId = value.CourseId,
            TeacherId = value.TeacherId,
            RoomId = value.RoomId,
            StartDate = value.StartDate,
            TotalSessions = value.TotalSessions,
            RowVersion = value.RowVersion,
            Schedules = value.Schedules.ToList()
        };
    }

    private async Task LoadStudentsAsync(int ownerUserId, int classId, CancellationToken cancellationToken)
    {
        var result = await _classEnrollmentService.GetEnrolledStudentsAsync(ownerUserId, classId, cancellationToken);
        if (result.IsSuccess && result.Value != null) EnrolledStudents = result.Value;
    }

    private int? GetCurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
}
