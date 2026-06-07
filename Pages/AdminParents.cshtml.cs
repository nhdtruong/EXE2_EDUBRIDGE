using System.Security.Claims;
using EduBridge.Contracts.Parents;
using EduBridge.Services.Parents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages;

[Authorize(Policy = "AdminOnly")]
public sealed class AdminParentsModel : PageModel
{
    private readonly IParentManagementService _service;
    public AdminParentsModel(IParentManagementService service) => _service = service;

    [BindProperty(SupportsGet = true)] public string? NameFilter { get; set; }
    [BindProperty(SupportsGet = true)] public string? EmailFilter { get; set; }
    [BindProperty(SupportsGet = true)] public string? PhoneFilter { get; set; }
    [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
    [BindProperty(SupportsGet = true)] public string? ChildrenFilter { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 20;
    public ParentPagedResponse Result { get; private set; } = new([], 1, 20, 0, 1);
    public string? ResetPasswordResult { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        ResetPasswordResult = TempData["ResetPasswordResult"] as string;
        var result = await _service.GetParentsAsync(GetUserId(), new ParentQuery
        {
            Name = NameFilter, Email = EmailFilter, PhoneNumber = PhoneFilter, Status = StatusFilter,
            HasChildren = ChildrenFilter == "yes" ? true : ChildrenFilter == "no" ? false : null,
            Page = PageNumber, PageSize = PageSize
        }, cancellationToken);
        if (!result.IsSuccess || result.Value == null) ModelState.AddModelError(string.Empty, result.Message);
        else Result = result.Value;
        return Page();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(int parentUserId, string currentStatus, CancellationToken cancellationToken)
    {
        var next = currentStatus == "Active" ? "Inactive" : "Active";
        var result = await _service.SetStatusAsync(GetUserId(), parentUserId, next, cancellationToken);
        TempData["ToastTitle"] = result.IsSuccess ? "Thành công" : "Thất bại";
        TempData["ToastType"] = result.IsSuccess ? "success" : "error";
        TempData["ToastMessage"] = result.Message;
        return RedirectToPage("/AdminParents", new { NameFilter, EmailFilter, PhoneFilter, StatusFilter, ChildrenFilter, PageNumber, PageSize });
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(int parentUserId, CancellationToken cancellationToken)
    {
        var result = await _service.ResetPasswordAsync(GetUserId(), parentUserId, cancellationToken);
        if (result.IsSuccess) TempData["ResetPasswordResult"] = result.Value!.TemporaryPassword;
        else
        {
            TempData["ToastTitle"] = "Thất bại";
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = result.Message;
        }
        return RedirectToPage("/AdminParents", new { NameFilter, EmailFilter, PhoneFilter, StatusFilter, ChildrenFilter, PageNumber, PageSize });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int parentUserId, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteParentAsync(GetUserId(), parentUserId, cancellationToken);
        TempData["ToastTitle"] = result.IsSuccess ? "Thành công" : "Thất bại";
        TempData["ToastType"] = result.IsSuccess ? "success" : "error";
        TempData["ToastMessage"] = result.Message;
        return RedirectToPage("/AdminParents", new { NameFilter, EmailFilter, PhoneFilter, StatusFilter, ChildrenFilter, PageNumber, PageSize });
    }

    private int GetUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
