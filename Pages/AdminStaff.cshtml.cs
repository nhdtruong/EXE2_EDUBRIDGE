using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EduBridge.Contracts.Staffs;
using EduBridge.Services.Staffs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages;

[Authorize(Policy = "AdminOnly")]
public class AdminStaffModel : PageModel
{
    private readonly IStaffManagementService _service;
    private readonly ILogger<AdminStaffModel> _logger;

    public AdminStaffModel(IStaffManagementService service, ILogger<AdminStaffModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true), MaxLength(150)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true), MaxLength(150)] public string? ContactSearch { get; set; }
    [BindProperty(SupportsGet = true), MaxLength(20)] public string? StatusFilter { get; set; }
    [BindProperty(SupportsGet = true), MaxLength(20)] public string? RoleFilter { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 20;

    public List<StaffListItem> Staffs { get; private set; } = [];
    public int[] PageSizeOptions { get; } = [10, 20, 50, 100, 200, 500];
    public int TotalStaffs { get; private set; }
    public int TotalPages => TotalStaffs == 0 ? 1 : (int)Math.Ceiling(TotalStaffs / (double)PageSize);
    public int FirstItemNumber => TotalStaffs == 0 ? 0 : (PageNumber - 1) * PageSize + 1;

    public string? ResetPasswordStaffName => TempData["ResetPasswordStaffName"] as string;
    public string? ResetPasswordValue => TempData["ResetPasswordValue"] as string;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return RedirectToPage("/Login");

        if (!PageSizeOptions.Contains(PageSize)) PageSize = 20;
        if (PageNumber < 1) PageNumber = 1;

        var query = new StaffQuery
        {
            Keyword = !string.IsNullOrWhiteSpace(Search) ? Search : ContactSearch,
            Status = StatusFilter,
            Role = RoleFilter,
            Page = PageNumber,
            PageSize = PageSize
        };

        var result = await _service.GetStaffsAsync(ownerUserId.Value, query, cancellationToken);
        if (!result.IsSuccess)
        {
            Staffs = [];
            return Page();
        }

        var response = result.Value!;
        TotalStaffs = response.TotalItems;
        PageNumber = response.Page;

        Staffs = response.Items.Select(t => new StaffListItem
        {
            StaffId = t.UserId,
            StaffCode = t.StaffCode,
            FullName = t.FullName,
            Roles = t.Roles,
            Email = t.Email ?? string.Empty,
            PhoneNumber = t.PhoneNumber ?? string.Empty,
            AvatarUrl = t.AvatarUrl,
            Specialization = t.Specialization,
            ClassCount = t.ClassCount,
            StudentCount = t.StudentCount,
            Status = t.Status,
            UserStatus = t.Status // The service normalizes user status properly
        }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(int staffId, CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return RedirectToPage("/Login");

        var staffResult = await _service.GetStaffAsync(ownerUserId.Value, staffId, cancellationToken);
        if (!staffResult.IsSuccess) return RedirectToPage();

        var newStatus = staffResult.Value!.Status == "Active" ? "Inactive" : "Active";
        await _service.SetStatusAsync(ownerUserId.Value, staffId, newStatus, cancellationToken);

        TempData["ToastType"] = "success";
        TempData["ToastTitle"] = "Thành công";
        TempData["ToastMessage"] = newStatus == "Active" ? "Đã chuyển trạng thái thành Hoạt động." : "Đã chuyển trạng thái thành Đã khóa.";

        return RedirectToPage(new { Search, ContactSearch, StatusFilter, RoleFilter, PageNumber, PageSize });
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(int staffId, CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return RedirectToPage("/Login");

        var staffResult = await _service.GetStaffAsync(ownerUserId.Value, staffId, cancellationToken);
        if (!staffResult.IsSuccess) return RedirectToPage();

        var resetResult = await _service.ResetPasswordAsync(ownerUserId.Value, staffId, cancellationToken);
        if (!resetResult.IsSuccess) return RedirectToPage();

        TempData["ResetPasswordStaffName"] = staffResult.Value!.FullName;
        TempData["ResetPasswordValue"] = resetResult.Value!.TemporaryPassword;

        return RedirectToPage(new { Search, ContactSearch, StatusFilter, RoleFilter, PageNumber, PageSize });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int staffId, CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return RedirectToPage("/Login");

        var result = await _service.DeleteStaffAsync(ownerUserId.Value, staffId, cancellationToken);

        TempData["ToastTitle"] = result.IsSuccess ? "Thành công" : "Lỗi";
        TempData["ToastType"] = result.IsSuccess ? "success" : "error";
        TempData["ToastMessage"] = result.Message;

        return RedirectToPage(new { Search, ContactSearch, StatusFilter, RoleFilter, PageNumber, PageSize });
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}

public sealed class StaffListItem
{
    public int StaffId { get; set; }
    public string StaffCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Specialization { get; set; }
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

    public string StatusText => IsActive ? "Hoạt động" : "Đã khóa";
}



