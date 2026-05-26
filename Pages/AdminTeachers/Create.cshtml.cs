using System.Data;
using EduBridge.Data;
using EduBridge.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Pages.AdminTeachers;

[Authorize(Policy = "AdminOnly")]
public sealed class CreateModel : TeacherProfilePageModel
{
    public CreateModel(AppDbContext context)
        : base(context)
    {
    }

    [BindProperty]
    public TeacherProfileInput Input { get; set; } = new();

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
            ModelState.AddModelError(string.Empty, "Không tìm thấy trung tâm đang hoạt động.");
            return Page();
        }

        Input.Normalize();
        await ValidateTeacherProfileAsync(Input, null, null, cancellationToken);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var teacherRoleId = await Context.Roles
            .AsNoTracking()
            .Where(r => r.RoleCode == "TEACHER")
            .Select(r => (int?)r.RoleId)
            .FirstOrDefaultAsync(cancellationToken);

        if (teacherRoleId == null)
        {
            ModelState.AddModelError(string.Empty, "Chưa cấu hình role TEACHER.");
            return Page();
        }

        string? uploadedAvatarUrl = null;

        try
        {
            uploadedAvatarUrl = await SaveAvatarAsync(
                Input.AvatarFile,
                Input.TeacherCode,
                cancellationToken);

            await using var transaction = await Context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var user = new User
            {
                RoleId = teacherRoleId.Value,
                FullName = Input.FullName,
                Email = Input.Email,
                PhoneNumber = Input.PhoneNumber,
                NormalizedPhoneNumber = NormalizePhoneNumber(Input.PhoneNumber),
                DateOfBirth = Input.DateOfBirth,
                Gender = Input.Gender,
                Ethnicity = Input.Ethnicity,
                Religion = Input.Religion,
                IdentityNumber = Input.IdentityNumber,
                IdentityIssuedDate = Input.IdentityIssuedDate,
                IdentityIssuedPlace = Input.IdentityIssuedPlace,
                CurrentAddress = Input.CurrentAddress,
                PermanentAddress = Input.PermanentAddress,
                Hometown = Input.Hometown,
                PlaceOfBirth = Input.PlaceOfBirth,
                AvatarUrl = uploadedAvatarUrl,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("edubridge2026"),
                EmailConfirmed = true,
                Status = Input.IsActive ? "Active" : "Inactive",
                CreatedAt = DateTime.UtcNow
            };

            Context.Users.Add(user);
            await Context.SaveChangesAsync(cancellationToken);

            Context.Teachers.Add(new EduBridge.Models.Teacher
            {
                UserId = user.UserId,
                CenterId = centerId.Value,
                TeacherCode = Input.TeacherCode,
                Status = Input.IsActive ? "Active" : "Inactive",
                Specialization = null,
                ExperienceYears = 0
            });

            await Context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            DeleteUploadedAvatar(uploadedAvatarUrl);
            ModelState.AddModelError(string.Empty, "Không thể thêm giáo viên. Vui lòng kiểm tra dữ liệu trùng.");
            return Page();
        }

        TempData["ToastType"] = "success";
        TempData["ToastTitle"] = "Thành công";
        TempData["ToastMessage"] = "Đã thêm giáo viên mới.";

        return RedirectToPage("/AdminTeachers");
    }
}
