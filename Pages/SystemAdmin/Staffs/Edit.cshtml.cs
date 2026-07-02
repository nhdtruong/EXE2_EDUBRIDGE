using System.Security.Claims;
using EduBridge.Contracts.SystemStaffs;
using EduBridge.Services.SystemStaffs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages.SystemAdmin.Staffs;

[Authorize(Policy = "SystemAdminOnly")]
public class EditModel : PageModel
{
    private readonly ISystemStaffService _service;

    public EditModel(ISystemStaffService service)
    {
        _service = service;
    }

    [BindProperty]
    public SaveSystemStaffRequest Input { get; set; } = new();

    [BindProperty]
    public IFormFile? AvatarFile { get; set; }

    [BindProperty]
    public bool RemoveAvatar { get; set; }

    public string? ExistingAvatarUrl { get; set; }

    public string? StaffName { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _service.GetStaffAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ToastType"] = "error";
            TempData["ToastTitle"] = "Lỗi";
            TempData["ToastMessage"] = result.Message;
            return RedirectToPage("/SystemAdmin/Staffs/Index");
        }

        var staff = result.Value!;
        StaffName = staff.FullName;
        ExistingAvatarUrl = staff.AvatarUrl;

        Input = new SaveSystemStaffRequest
        {
            FullName = staff.FullName,
            Email = staff.Email ?? string.Empty,
            PhoneNumber = staff.PhoneNumber ?? string.Empty,
            StaffCode = staff.StaffCode ?? string.Empty,
            DateOfBirth = staff.DateOfBirth,
            Gender = staff.Gender ?? "Nam",
            IdentityNumber = staff.IdentityNumber ?? string.Empty,
            IdentityIssuedDate = staff.IdentityIssuedDate,
            IdentityIssuedPlace = staff.IdentityIssuedPlace,
            Ethnicity = staff.Ethnicity,
            Religion = staff.Religion,
            CurrentAddress = staff.CurrentAddress,
            PermanentAddress = staff.PermanentAddress,
            Hometown = staff.Hometown,
            PlaceOfBirth = staff.PlaceOfBirth,
            IsActive = staff.Status?.ToUpper() == "ACTIVE"
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return RedirectToPage("/Login");

        var result = await _service.UpdateAsync(currentUserId.Value, id, Input, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            
            TempData["ToastType"] = "error";
            TempData["ToastTitle"] = "Thất bại";
            TempData["ToastMessage"] = result.Message;
            
            return Page();
        }

        if (RemoveAvatar)
        {
            await _service.RemoveAvatarAsync(currentUserId.Value, id, cancellationToken);
        }
        else if (AvatarFile != null)
        {
            await using var stream = AvatarFile.OpenReadStream();
            await _service.UpdateAvatarAsync(currentUserId.Value, id, stream, AvatarFile.FileName, AvatarFile.ContentType, cancellationToken);
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
