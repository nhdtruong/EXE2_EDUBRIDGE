using System.Security.Claims;
using EduBridge.Contracts.Teachers;
using EduBridge.Services.Teachers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages.AdminTeachers;

[Authorize(Policy = "AdminOnly")]
public class CreateModel : PageModel
{
    private readonly ITeacherManagementService _service;

    public CreateModel(ITeacherManagementService service)
    {
        _service = service;
    }

    [BindProperty]
    public SaveTeacherRequest Input { get; set; } = new();

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
            return Page();
        }

        if (AvatarFile != null)
        {
            var teacherUserId = result.Value!.UserId;
            await using var stream = AvatarFile.OpenReadStream();
            await _service.UpdateAvatarAsync(ownerUserId.Value, teacherUserId, stream, AvatarFile.FileName, AvatarFile.ContentType, cancellationToken);
        }

        TempData["ToastType"] = "success";
        TempData["ToastTitle"] = "Thành công";
        TempData["ToastMessage"] = result.Message;

        return RedirectToPage("/AdminTeachers");
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
