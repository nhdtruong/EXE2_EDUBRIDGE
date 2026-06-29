using EduBridge.Contracts.Users;
using EduBridge.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace EduBridge.Pages;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly IUserProfileService _profileService;

    public ProfileModel(IUserProfileService profileService)
    {
        _profileService = profileService;
    }

    [BindProperty]
    public UpdateProfileRequest Input { get; set; } = new();

    [BindProperty]
    public IFormFile? AvatarFile { get; set; }

    public string? Email { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    private int? GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(idClaim, out var id)) return id;
        return null;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return RedirectToPage("/Login");

        var result = await _profileService.GetProfileAsync(userId.Value, cancellationToken);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            return RedirectToPage("/Index");
        }

        var profile = result.Value;
        Email = profile.Email;

        Input = new UpdateProfileRequest
        {
            FullName = profile.FullName,
            Email = profile.Email ?? "",
            PhoneNumber = profile.PhoneNumber ?? "",
            DateOfBirth = profile.DateOfBirth ?? default,
            Gender = profile.Gender ?? "",
            IdentityNumber = profile.IdentityNumber ?? "",
            IdentityIssuedDate = profile.IdentityIssuedDate,
            IdentityIssuedPlace = profile.IdentityIssuedPlace,
            CurrentAddress = profile.CurrentAddress,
            PermanentAddress = profile.PermanentAddress,
            Hometown = profile.Hometown,
            PlaceOfBirth = profile.PlaceOfBirth,
            Ethnicity = profile.Ethnicity,
            Religion = profile.Religion,
            AvatarUrl = profile.AvatarUrl
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return RedirectToPage("/Login");

        if (!ModelState.IsValid)
        {
            var result = await _profileService.GetProfileAsync(userId.Value, cancellationToken);
            if (result.IsSuccess) Email = result.Value.Email;
            return Page();
        }

        var updateResult = await _profileService.UpdateProfileAsync(userId.Value, Input, cancellationToken);
        if (!updateResult.IsSuccess)
        {
            ErrorMessage = updateResult.Message;
            var result = await _profileService.GetProfileAsync(userId.Value, cancellationToken);
            if (result.IsSuccess) Email = result.Value.Email;
            return Page();
        }

        if (AvatarFile != null)
        {
            await using var stream = AvatarFile.OpenReadStream();
            await _profileService.UpdateAvatarAsync(userId.Value, stream, AvatarFile.FileName, AvatarFile.ContentType, cancellationToken);
        }

        TempData["ToastTitle"] = "Thành công";
        TempData["ToastMessage"] = "Cập nhật hồ sơ cá nhân thành công.";
        TempData["ToastType"] = "success";
        
        return RedirectToPage("/Index");
    }
}