using System.Security.Claims;
using EduBridge.Contracts.Staffs;
using EduBridge.Services.Staffs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages.AdminStaff;

[Authorize(Policy = "AdminOnly")]
public class CreateModel : PageModel
{
    private readonly IStaffManagementService _service;

    public CreateModel(IStaffManagementService service)
    {
        _service = service;
    }

    [BindProperty]
    public SaveStaffRequest Input { get; set; } = new();

    [BindProperty]
    public IFormFile? AvatarFile { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return RedirectToPage("/Login");

        var result = await _service.CreateAsync(ownerUserId.Value, Input, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Errors != null)
            {
                foreach (var (key, errors) in result.Errors)
                foreach (var error in errors)
                    ModelState.AddModelError(string.IsNullOrEmpty(key) ? string.Empty : $"Input.{key}", error);
            }
            else ModelState.AddModelError(string.Empty, result.Message);
            
            TempData["ToastType"] = "error";
            TempData["ToastTitle"] = "Thất bại";
            TempData["ToastMessage"] = result.Message;
            
            return Page();
        }

        if (AvatarFile != null)
        {
            var staffUserId = result.Value!.UserId;
            await using var stream = AvatarFile.OpenReadStream();
            await _service.UpdateAvatarAsync(ownerUserId.Value, staffUserId, stream, AvatarFile.FileName, AvatarFile.ContentType, cancellationToken);
        }

        TempData["ToastType"] = "success";
        TempData["ToastTitle"] = "Thành công";
        TempData["ToastMessage"] = result.Message;

        return RedirectToPage("/AdminStaff");
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}

