using System.Data;
using System.Security.Claims;
using System.Text.RegularExpressions;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Services.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Pages.AdminStudents;

[Authorize(Policy = "AdminOnly")]
public sealed class EditModel : PageModel
{
    private readonly IStudentManagementService _studentService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        IStudentManagementService studentService,
        ILogger<EditModel> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    [BindProperty]
    public StudentProfileInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(
        int studentId,
        CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return RedirectToPage("/Login");

        var result = await _studentService.GetStudentAsync(ownerUserId.Value, studentId, cancellationToken);
        if (!result.IsSuccess || result.Value == null)
        {
            return NotFound();
        }

        PopulateInput(result.Value);
        return Page();
    }

    public async Task<IActionResult> OnGetSearchParentsAsync(
        string? keyword,
        CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null)
        {
            return new JsonResult(new { success = false, message = "Phiên đăng nhập đã hết hạn." })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
        }

        var result = await _studentService.SearchParentsAsync(ownerUserId.Value, keyword ?? string.Empty, cancellationToken);
        if (!result.IsSuccess)
        {
            return new JsonResult(new { success = false, message = result.Message })
            {
                StatusCode = result.Message.Contains("Không tìm thấy trung tâm") ? StatusCodes.Status403Forbidden : StatusCodes.Status400BadRequest
            };
        }

        return new JsonResult(new
        {
            success = true,
            found = result.Value?.Count > 0,
            parents = result.Value
        });
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return RedirectToPage("/Login");

        Input.Normalize();
        ModelState.Clear();
        TryValidateModel(Input, nameof(Input));

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var request = new EduBridge.Contracts.Students.UpdateStudentRequest
        {
            FullName = Input.FullName,
            DateOfBirth = Input.DateOfBirth,
            Gender = Input.Gender,
            Ethnicity = Input.Ethnicity,
            Religion = Input.Religion,
            IdentityNumber = Input.IdentityNumber,
            IdentityIssuedDate = Input.IdentityIssuedDate,
            IdentityIssuedPlace = Input.IdentityIssuedPlace,
            CurrentAddress = Input.CurrentAddress,
            PermanentAddress = Input.PermanentAddress,
            Hometown = Input.Hometown,
            PlaceOfBirth = Input.PlaceOfBirth,
            StudentPhoneNumber = Input.StudentPhoneNumber,
            StudentEmail = Input.StudentEmail,
            AvatarFile = Input.AvatarFile,
            RemoveAvatar = Input.RemoveAvatar
        };

        var result = await _studentService.UpdateStudentAsync(ownerUserId.Value, Input.StudentId, request, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return Page();
        }

        if (Input.ParentUserId.HasValue)
        {
            var parentResult = await _studentService.UpdateStudentParentAsync(ownerUserId.Value, Input.StudentId, new EduBridge.Contracts.Students.UpdateStudentParentRequest { ParentUserId = Input.ParentUserId.Value }, cancellationToken);
            if (!parentResult.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, parentResult.Message);
                return Page();
            }
        }

        var statusResult = await _studentService.GetStudentAsync(ownerUserId.Value, Input.StudentId, cancellationToken);
        if (statusResult.IsSuccess && statusResult.Value != null)
        {
            bool currentIsActive = statusResult.Value.Status.Equals("Active", StringComparison.OrdinalIgnoreCase);
            if (currentIsActive != Input.IsActive)
            {
                var toggleResult = await _studentService.ToggleStudentStatusAsync(ownerUserId.Value, Input.StudentId, cancellationToken);
                if (!toggleResult.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, toggleResult.Message);
                    return Page();
                }
            }
        }

        TempData["ToastType"] = "success";
        TempData["ToastTitle"] = "Thành công";
        TempData["ToastMessage"] = "Đã cập nhật thông tin học sinh.";

        return RedirectToPage("/AdminStudents");
    }

    private void PopulateInput(EduBridge.Contracts.Students.StudentResponse student)
    {
        Input = new StudentProfileInput
        {
            StudentId = student.StudentId,
            StudentCode = student.StudentCode,
            FullName = student.FullName,
            DateOfBirth = student.DateOfBirth,
            Gender = string.IsNullOrWhiteSpace(student.Gender) ? "Nam" : student.Gender,
            CurrentAddress = student.CurrentAddress,
            StudentPhoneNumber = student.PhoneNumber,
            StudentEmail = student.Email,
            IsActive = student.Status.Equals("Active", StringComparison.OrdinalIgnoreCase),
            AvatarUrl = student.AvatarUrl,
            ParentUserId = student.ParentUserId,
            ParentFullName = student.ParentName,
            ParentPhoneNumber = student.ParentPhone ?? string.Empty,
            ParentEmail = student.ParentEmail
        };

        Input.Ethnicity = student.Ethnicity;
        Input.Religion = student.Religion;
        Input.IdentityNumber = student.IdentityNumber;
        Input.IdentityIssuedDate = student.IdentityIssuedDate;
        Input.IdentityIssuedPlace = student.IdentityIssuedPlace;
        Input.PermanentAddress = student.PermanentAddress;
        Input.Hometown = student.Hometown;
        Input.PlaceOfBirth = student.PlaceOfBirth;
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var userId)
            ? userId
            : null;
    }
}
