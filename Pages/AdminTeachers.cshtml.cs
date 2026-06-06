using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EduBridge.Contracts.Teachers;
using EduBridge.Services.Teachers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages;

[Authorize(Policy = "AdminOnly")]
public class AdminTeachersModel : PageModel
{
    private readonly ITeacherManagementService _service;
    private readonly ILogger<AdminTeachersModel> _logger;

    public AdminTeachersModel(ITeacherManagementService service, ILogger<AdminTeachersModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true), MaxLength(150)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true), MaxLength(150)] public string? ContactSearch { get; set; }
    [BindProperty(SupportsGet = true), MaxLength(20)] public string? StatusFilter { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 20;

    public List<TeacherListItem> Teachers { get; private set; } = [];
    public int[] PageSizeOptions { get; } = [10, 20, 50, 100, 200, 500];
    public int TotalTeachers { get; private set; }
    public int TotalPages => TotalTeachers == 0 ? 1 : (int)Math.Ceiling(TotalTeachers / (double)PageSize);
    public int FirstItemNumber => TotalTeachers == 0 ? 0 : (PageNumber - 1) * PageSize + 1;

    public string? ResetPasswordTeacherName => TempData["ResetPasswordTeacherName"] as string;
    public string? ResetPasswordValue => TempData["ResetPasswordValue"] as string;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return RedirectToPage("/Login");

        if (!PageSizeOptions.Contains(PageSize)) PageSize = 20;
        if (PageNumber < 1) PageNumber = 1;

        var query = new TeacherQuery
        {
            Keyword = !string.IsNullOrWhiteSpace(Search) ? Search : ContactSearch,
            Status = StatusFilter,
            Page = PageNumber,
            PageSize = PageSize
        };

        var result = await _service.GetTeachersAsync(ownerUserId.Value, query, cancellationToken);
        if (!result.IsSuccess)
        {
            Teachers = [];
            return Page();
        }

        var response = result.Value!;
        TotalTeachers = response.TotalItems;
        PageNumber = response.Page;

        Teachers = response.Items.Select(t => new TeacherListItem
        {
            TeacherId = t.UserId,
            TeacherCode = t.TeacherCode,
            FullName = t.FullName,
            Email = t.Email ?? string.Empty,
            PhoneNumber = t.PhoneNumber ?? string.Empty,
            AvatarUrl = t.AvatarUrl,
            ClassCount = t.ClassCount,
            StudentCount = t.StudentCount,
            Status = t.Status,
            UserStatus = t.Status // The service normalizes user status properly
        }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(int teacherId, CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return RedirectToPage("/Login");

        var teacherResult = await _service.GetTeacherAsync(ownerUserId.Value, teacherId, cancellationToken);
        if (!teacherResult.IsSuccess) return RedirectToPage();

        var newStatus = teacherResult.Value!.Status == "Active" ? "Inactive" : "Active";
        await _service.SetStatusAsync(ownerUserId.Value, teacherId, newStatus, cancellationToken);

        TempData["ToastType"] = "success";
        TempData["ToastTitle"] = "Thành công";
        TempData["ToastMessage"] = newStatus == "Active" ? "Đã mở lại hoạt động cho giáo viên." : "Đã tạm dừng hoạt động giáo viên.";

        return RedirectToPage(new { Search, ContactSearch, StatusFilter, PageNumber, PageSize });
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(int teacherId, CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return RedirectToPage("/Login");

        var teacherResult = await _service.GetTeacherAsync(ownerUserId.Value, teacherId, cancellationToken);
        if (!teacherResult.IsSuccess) return RedirectToPage();

        var resetResult = await _service.ResetPasswordAsync(ownerUserId.Value, teacherId, cancellationToken);
        if (!resetResult.IsSuccess) return RedirectToPage();

        TempData["ResetPasswordTeacherName"] = teacherResult.Value!.FullName;
        TempData["ResetPasswordValue"] = resetResult.Value!.TemporaryPassword;

        return RedirectToPage(new { Search, ContactSearch, StatusFilter, PageNumber, PageSize });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int teacherId, CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return RedirectToPage("/Login");

        var result = await _service.DeleteTeacherAsync(ownerUserId.Value, teacherId, cancellationToken);

        TempData["ToastTitle"] = result.IsSuccess ? "Thành công" : "Lỗi";
        TempData["ToastType"] = result.IsSuccess ? "success" : "error";
        TempData["ToastMessage"] = result.Message;

        return RedirectToPage(new { Search, ContactSearch, StatusFilter, PageNumber, PageSize });
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}

public sealed class TeacherListItem
{
    public int TeacherId { get; set; }
    public string TeacherCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int ClassCount { get; set; }
    public int StudentCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string UserStatus { get; set; } = string.Empty;

    public bool IsActive => Status.Equals("Active", StringComparison.OrdinalIgnoreCase) && UserStatus.Equals("Active", StringComparison.OrdinalIgnoreCase);
    public string DisplayPhoneNumber => string.IsNullOrWhiteSpace(PhoneNumber) ? "-" : PhoneNumber;
    public string DisplayEmail => string.IsNullOrWhiteSpace(Email) ? "-" : Email;

    public string Initials
    {
        get
        {
            var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0) return "GV";
            if (parts.Length == 1) return parts[0].Length >= 2 ? parts[0][..2].ToUpperInvariant() : parts[0].ToUpperInvariant();
            return $"{parts[^2][0]}{parts[^1][0]}".ToUpperInvariant();
        }
    }

    public string StatusText => IsActive ? "Đang dạy" : "Tạm dừng";
}
