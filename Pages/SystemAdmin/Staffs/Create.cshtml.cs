using System.Security.Claims;
using EduBridge.Contracts.SystemStaffs;
using EduBridge.Services.SystemStaffs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages.SystemAdmin.Staffs;

[Authorize(Policy = "SystemAdminOnly")]
public class CreateModel : PageModel
{
    private readonly ISystemStaffService _service;

    public CreateModel(ISystemStaffService service)
    {
        _service = service;
    }

    [BindProperty]
    public SaveSystemStaffRequest Input { get; set; } = new();

    [BindProperty]
    public IFormFile? AvatarFile { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return RedirectToPage("/Login");

        var result = await _service.CreateAsync(currentUserId.Value, Input, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            
            TempData["ToastType"] = "error";
            TempData["ToastTitle"] = "Thất bại";
            TempData["ToastMessage"] = result.Message;
            
            return Page();
        }

        if (AvatarFile != null)
        {
            var staffUserId = result.Value!.UserId;
            await using var stream = AvatarFile.OpenReadStream();
            await _service.UpdateAvatarAsync(currentUserId.Value, staffUserId, stream, AvatarFile.FileName, AvatarFile.ContentType, cancellationToken);
        }

        TempData["ToastType"] = "success";
        TempData["ToastTitle"] = "Thành công";
        TempData["ToastMessage"] = result.Message;

        return RedirectToPage("/SystemAdmin/Staffs/Index");
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
