using EduBridge.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Pages.AdminTeachers;

[Authorize(Policy = "AdminOnly")]
public sealed class EditModel : TeacherProfilePageModel
{
    public EditModel(AppDbContext context)
        : base(context)
    {
    }

    [BindProperty(SupportsGet = true)]
    public int TeacherId { get; set; }

    [BindProperty]
    public TeacherProfileInput Input { get; set; } = new();

    public string? CurrentAvatarUrl { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var loadResult = await LoadTeacherAsync(cancellationToken);

        return loadResult ?? Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();

        if (ownerUserId == null)
        {
            return RedirectToPage("/Login");
        }

        var centerId = await GetOwnerCenterIdAsync(ownerUserId.Value, cancellationToken);

        if (centerId == null)
        {
            return Forbid();
        }

        var teacher = await Context.Teachers
            .Include(t => t.User)
            .FirstOrDefaultAsync(
                t => t.TeacherId == TeacherId && t.CenterId == centerId.Value,
                cancellationToken);

        if (teacher == null)
        {
            return NotFound();
        }

        Input.Normalize();
        await ValidateTeacherProfileAsync(
            Input,
            teacher.TeacherId,
            teacher.UserId,
            cancellationToken);

        CurrentAvatarUrl = teacher.User.AvatarUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var oldAvatarUrl = teacher.User.AvatarUrl;
        string? uploadedAvatarUrl = null;

        try
        {
            uploadedAvatarUrl = await SaveAvatarAsync(
                Input.AvatarFile,
                Input.TeacherCode,
                cancellationToken);

            teacher.TeacherCode = Input.TeacherCode;
            teacher.Status = Input.IsActive ? "Active" : "Inactive";
            teacher.User.Status = Input.IsActive ? "Active" : "Inactive";
            teacher.User.FullName = Input.FullName;
            teacher.User.Email = Input.Email;
            teacher.User.PhoneNumber = Input.PhoneNumber;
            teacher.User.NormalizedPhoneNumber = NormalizePhoneNumber(Input.PhoneNumber);
            teacher.User.DateOfBirth = Input.DateOfBirth;
            teacher.User.Gender = Input.Gender;
            teacher.User.Ethnicity = Input.Ethnicity;
            teacher.User.Religion = Input.Religion;
            teacher.User.IdentityNumber = Input.IdentityNumber;
            teacher.User.IdentityIssuedDate = Input.IdentityIssuedDate;
            teacher.User.IdentityIssuedPlace = Input.IdentityIssuedPlace;
            teacher.User.CurrentAddress = Input.CurrentAddress;
            teacher.User.PermanentAddress = Input.PermanentAddress;
            teacher.User.Hometown = Input.Hometown;
            teacher.User.PlaceOfBirth = Input.PlaceOfBirth;

            if (Input.RemoveAvatar)
            {
                teacher.User.AvatarUrl = null;
            }

            if (!string.IsNullOrWhiteSpace(uploadedAvatarUrl))
            {
                teacher.User.AvatarUrl = uploadedAvatarUrl;
            }

            await Context.SaveChangesAsync(cancellationToken);

            if (!string.Equals(oldAvatarUrl, teacher.User.AvatarUrl, StringComparison.OrdinalIgnoreCase))
            {
                DeleteUploadedAvatar(oldAvatarUrl);
            }
        }
        catch (DbUpdateException)
        {
            DeleteUploadedAvatar(uploadedAvatarUrl);
            ModelState.AddModelError(string.Empty, "Không thể cập nhật giáo viên. Vui lòng kiểm tra dữ liệu trùng.");
            CurrentAvatarUrl = oldAvatarUrl;
            return Page();
        }

        TempData["ToastType"] = "success";
        TempData["ToastTitle"] = "Thành công";
        TempData["ToastMessage"] = "Đã cập nhật thông tin giáo viên.";

        return RedirectToPage("/AdminTeachers");
    }

    private async Task<IActionResult?> LoadTeacherAsync(CancellationToken cancellationToken)
    {
        var ownerUserId = GetCurrentUserId();

        if (ownerUserId == null)
        {
            return RedirectToPage("/Login");
        }

        var centerId = await GetOwnerCenterIdAsync(ownerUserId.Value, cancellationToken);

        if (centerId == null)
        {
            return Forbid();
        }

        var teacher = await Context.Teachers
            .AsNoTracking()
            .Where(t => t.TeacherId == TeacherId && t.CenterId == centerId.Value)
            .Select(t => new
            {
                t.TeacherId,
                t.TeacherCode,
                TeacherStatus = t.Status,
                t.User.FullName,
                t.User.DateOfBirth,
                t.User.Gender,
                t.User.Ethnicity,
                t.User.Religion,
                t.User.IdentityNumber,
                t.User.IdentityIssuedDate,
                t.User.IdentityIssuedPlace,
                t.User.CurrentAddress,
                t.User.PermanentAddress,
                t.User.Hometown,
                t.User.PlaceOfBirth,
                t.User.PhoneNumber,
                t.User.Email,
                t.User.AvatarUrl
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (teacher == null)
        {
            return NotFound();
        }

        Input = new TeacherProfileInput
        {
            TeacherCode = teacher.TeacherCode,
            FullName = teacher.FullName,
            DateOfBirth = teacher.DateOfBirth,
            Gender = string.IsNullOrWhiteSpace(teacher.Gender) ? "Nam" : teacher.Gender,
            Ethnicity = teacher.Ethnicity,
            Religion = teacher.Religion,
            IdentityNumber = teacher.IdentityNumber ?? string.Empty,
            IdentityIssuedDate = teacher.IdentityIssuedDate,
            IdentityIssuedPlace = teacher.IdentityIssuedPlace,
            CurrentAddress = teacher.CurrentAddress,
            PermanentAddress = teacher.PermanentAddress,
            Hometown = teacher.Hometown,
            PlaceOfBirth = teacher.PlaceOfBirth,
            PhoneNumber = teacher.PhoneNumber ?? string.Empty,
            Email = teacher.Email,
            IsActive = teacher.TeacherStatus == "Active"
        };
        CurrentAvatarUrl = teacher.AvatarUrl;

        return null;
    }
}
