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
public sealed class CreateModel : PageModel
{
    private readonly IStudentManagementService _studentService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        IStudentManagementService studentService,
        ILogger<CreateModel> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    [BindProperty]
    public StudentCreateInput Input { get; set; } = new();

    public IActionResult OnGet()
    {
        var ownerUserId = GetCurrentUserId();

        if (ownerUserId == null)
        {
            return RedirectToPage("/Login");
        }

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

        var request = new EduBridge.Contracts.Students.SaveStudentRequest
        {
            StudentCode = Input.StudentCode,
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
            ParentUserId = Input.ParentUserId,
            ParentFullName = Input.ParentFullName,
            ParentPhoneNumber = Input.ParentPhoneNumber,
            ParentEmail = Input.ParentEmail,
            AvatarFile = Input.AvatarFile
        };

        var result = await _studentService.CreateStudentAsync(ownerUserId.Value, request, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return Page();
        }

        TempData["ToastType"] = "success";
        TempData["ToastTitle"] = "Thành công";
        TempData["ToastMessage"] = "Đã thêm mới học sinh.";

        return RedirectToPage("/AdminStudents");
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var userId)
            ? userId
            : null;
    }
}
