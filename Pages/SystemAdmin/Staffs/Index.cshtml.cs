using EduBridge.Contracts.SystemStaffs;
using EduBridge.Services.SystemStaffs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace EduBridge.Pages.SystemAdmin.Staffs;

[Authorize(Policy = "SystemAdminOnly")]
public class IndexModel : PageModel
{
    private readonly ISystemStaffService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ISystemStaffService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true), MaxLength(150)] public string? NameCodeSearch { get; set; }
    [BindProperty(SupportsGet = true), MaxLength(150)] public string? EmailPhoneSearch { get; set; }
    [BindProperty(SupportsGet = true), MaxLength(20)] public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 20;

    public List<SystemStaffListItemResponse> Staffs { get; private set; } = [];
    public int[] PageSizeOptions { get; } = [10, 20, 50, 100];
    public int TotalStaffs { get; private set; }
    public int TotalPages => TotalStaffs == 0 ? 1 : (int)Math.Ceiling(TotalStaffs / (double)PageSize);
    public int FirstItemNumber => TotalStaffs == 0 ? 0 : (PageNumber - 1) * PageSize + 1;

    public string? ResetPasswordStaffName => TempData["ResetPasswordStaffName"] as string;
    public string? ResetPasswordValue => TempData["ResetPasswordValue"] as string;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return RedirectToPage("/Login");

        if (!PageSizeOptions.Contains(PageSize)) PageSize = 20;
        if (PageNumber < 1) PageNumber = 1;

        var query = new SystemStaffQuery
        {
            NameCodeKeyword = NameCodeSearch,
            EmailPhoneKeyword = EmailPhoneSearch,
            Status = StatusFilter,

            Page = PageNumber,
            PageSize = PageSize
        };

        var result = await _service.GetStaffsAsync(query, cancellationToken);
        if (!result.IsSuccess)
        {
            Staffs = [];
            return Page();
        }

        var response = result.Value!;
        TotalStaffs = response.TotalItems;
        PageNumber = response.Page;
        Staffs = response.Items.ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(int staffId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return RedirectToPage("/Login");

        var staffResult = await _service.GetStaffAsync(staffId, cancellationToken);
        if (!staffResult.IsSuccess) return RedirectToPage();

        var newStatus = staffResult.Value!.Status?.ToUpper() == "ACTIVE" ? "INACTIVE" : "ACTIVE";
        var toggleResult = await _service.SetStatusAsync(currentUserId.Value, staffId, newStatus, cancellationToken);

        if (!toggleResult.IsSuccess)
        {
            TempData["ToastType"] = "error";
            TempData["ToastTitle"] = "Thất bại";
            TempData["ToastMessage"] = toggleResult.Message;
        }
        else
        {
            TempData["ToastType"] = "success";
            TempData["ToastTitle"] = "Thành công";
            TempData["ToastMessage"] = newStatus == "ACTIVE" ? "Đã chuyển trạng thái thành Hoạt động." : "Đã chuyển trạng thái thành Đã khóa.";
        }

        return RedirectToPage(new { NameCodeSearch, EmailPhoneSearch, StatusFilter, PageNumber, PageSize });
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(int staffId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return RedirectToPage("/Login");

        var staffResult = await _service.GetStaffAsync(staffId, cancellationToken);
        if (!staffResult.IsSuccess) return RedirectToPage();

        var resetResult = await _service.ResetPasswordAsync(currentUserId.Value, staffId, cancellationToken);
        if (!resetResult.IsSuccess)
        {
            TempData["ToastType"] = "error";
            TempData["ToastTitle"] = "Lỗi";
            TempData["ToastMessage"] = resetResult.Message;
            return RedirectToPage();
        }

        TempData["ResetPasswordStaffName"] = staffResult.Value!.FullName;
        TempData["ResetPasswordValue"] = resetResult.Value!.TemporaryPassword;

        return RedirectToPage(new { NameCodeSearch, EmailPhoneSearch, StatusFilter, PageNumber, PageSize });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int staffId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return RedirectToPage("/Login");

        var result = await _service.DeleteStaffAsync(currentUserId.Value, staffId, cancellationToken);

        TempData["ToastTitle"] = result.IsSuccess ? "Thành công" : "Lỗi";
        TempData["ToastType"] = result.IsSuccess ? "success" : "error";
        TempData["ToastMessage"] = result.Message;

        return RedirectToPage(new { NameCodeSearch, EmailPhoneSearch, StatusFilter, PageNumber, PageSize });
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
