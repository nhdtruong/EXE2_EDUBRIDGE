using System.Security.Claims;
using EduBridge.Contracts.Teachers;
using EduBridge.Services.Teachers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages.AdminTeachers;

[Authorize(Policy = "AdminOnly")]
public class EditModel : PageModel
{
    private readonly ITeacherManagementService _service;

    public EditModel(ITeacherManagementService service)
    {
        _service = service;
    }

    [BindProperty]
    public SaveTeacherRequest Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int TeacherId { get; set; }

    [BindProperty]
    public IFormFile? AvatarFile { get; set; }

    [BindProperty]
    public bool RemoveAvatar { get; set; }

    public string? ExistingAvatarUrl { get; set; }

    public async Task<IActionResult> OnGetAsync(int teacherId, CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return RedirectToPage("/Login");

        var result = await _service.GetTeacherAsync(ownerUserId.Value, teacherId, cancellationToken);
        if (!result.IsSuccess) return NotFound();

        var t = result.Value!;
        TeacherId = teacherId;
        ExistingAvatarUrl = t.AvatarUrl;

        Input = new SaveTeacherRequest
        {
            TeacherCode = t.TeacherCode,
            FullName = t.FullName,
            PhoneNumber = t.PhoneNumber ?? "",
            Email = t.Email,
            DateOfBirth = t.DateOfBirth,
            Gender = t.Gender,
            Ethnicity = t.Ethnicity,
            Religion = t.Religion,
            IdentityNumber = t.IdentityNumber,
            IdentityIssuedDate = t.IdentityIssuedDate,
            IdentityIssuedPlace = t.IdentityIssuedPlace,
            CurrentAddress = t.CurrentAddress,
            PermanentAddress = t.PermanentAddress,
            Hometown = t.Hometown,
            PlaceOfBirth = t.PlaceOfBirth,
            IsActive = t.Status == "Active"
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            // Re-fetch avatar url so the UI doesn't break
            var oId = GetCurrentUserId();
            if (oId != null)
            {
                var tr = await _service.GetTeacherAsync(oId.Value, TeacherId, cancellationToken);
                if (tr.IsSuccess) ExistingAvatarUrl = tr.Value!.AvatarUrl;
            }
            return Page();
        }

        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return RedirectToPage("/Login");

        var result = await _service.UpdateAsync(ownerUserId.Value, TeacherId, Input, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Errors != null)
            {
                foreach (var (key, errors) in result.Errors)
                foreach (var error in errors)
                    ModelState.AddModelError(string.IsNullOrEmpty(key) ? string.Empty : $"Input.{key}", error);
            }
            else ModelState.AddModelError(string.Empty, result.Message);

            var tr = await _service.GetTeacherAsync(ownerUserId.Value, TeacherId, cancellationToken);
            if (tr.IsSuccess) ExistingAvatarUrl = tr.Value!.AvatarUrl;

            return Page();
        }

        if (RemoveAvatar)
        {
            await _service.RemoveAvatarAsync(ownerUserId.Value, TeacherId, cancellationToken);
        }
        else if (AvatarFile != null)
        {
            await using var stream = AvatarFile.OpenReadStream();
            await _service.UpdateAvatarAsync(ownerUserId.Value, TeacherId, stream, AvatarFile.FileName, AvatarFile.ContentType, cancellationToken);
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
