using System.Security.Claims;
using System.Text.RegularExpressions;
using EduBridge.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Pages.AdminTeachers;

public abstract class TeacherProfilePageModel : PageModel
{
    protected TeacherProfilePageModel(AppDbContext context)
    {
        Context = context;
    }

    protected AppDbContext Context { get; }

    protected int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var userId)
            ? userId
            : null;
    }

    protected async Task<int?> GetOwnerCenterIdAsync(
        int ownerUserId,
        CancellationToken cancellationToken)
    {
        return await Context.Centers
            .AsNoTracking()
            .Where(c => c.OwnerUserId == ownerUserId && c.Status == "Active")
            .Select(c => (int?)c.CenterId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    protected async Task ValidateTeacherProfileAsync(
        TeacherProfileInput input,
        int? excludedTeacherId,
        int? excludedUserId,
        CancellationToken cancellationToken)
    {
        if (!IsValidTeacherCode(input.TeacherCode))
        {
            ModelState.AddModelError("Input.TeacherCode", "Mã giáo viên chỉ gồm chữ, số, dấu gạch ngang và tối đa 30 ký tự.");
        }

        if (!IsValidFullName(input.FullName))
        {
            ModelState.AddModelError("Input.FullName", "Họ và tên phải có ít nhất 2 ký tự và không chứa số hoặc ký tự đặc biệt.");
        }

        if (input.DateOfBirth == null)
        {
            ModelState.AddModelError("Input.DateOfBirth", "Vui lòng chọn ngày sinh.");
        }
        else
        {
            var today = GetVietnamToday();

            if (input.DateOfBirth > today)
            {
                ModelState.AddModelError("Input.DateOfBirth", "Ngày sinh không được lớn hơn ngày hiện tại.");
            }

            if (input.DateOfBirth < new DateOnly(1900, 1, 1))
            {
                ModelState.AddModelError("Input.DateOfBirth", "Ngày sinh không được trước 01/01/1900.");
            }

            if (input.DateOfBirth > today.AddYears(-18))
            {
                ModelState.AddModelError("Input.DateOfBirth", "Giáo viên phải đủ ít nhất 18 tuổi.");
            }
        }

        if (input.Gender is not ("Nam" or "Nữ"))
        {
            ModelState.AddModelError("Input.Gender", "Giới tính không hợp lệ.");
        }

        if (!IsValidIdentityNumber(input.IdentityNumber))
        {
            ModelState.AddModelError("Input.IdentityNumber", "CMND/CCCD phải gồm 9 hoặc 12 số.");
        }

        if (input.IdentityIssuedDate != null)
        {
            var today = GetVietnamToday();

            if (input.IdentityIssuedDate > today)
            {
                ModelState.AddModelError("Input.IdentityIssuedDate", "Ngày cấp không được lớn hơn ngày hiện tại.");
            }

            if (input.DateOfBirth != null && input.IdentityIssuedDate < input.DateOfBirth)
            {
                ModelState.AddModelError("Input.IdentityIssuedDate", "Ngày cấp không được nhỏ hơn ngày sinh.");
            }
        }

        ValidateOptionalText(input.Ethnicity, "Input.Ethnicity", "Dân tộc");
        ValidateOptionalText(input.Religion, "Input.Religion", "Tôn giáo");
        ValidateOptionalText(input.IdentityIssuedPlace, "Input.IdentityIssuedPlace", "Nơi cấp");
        ValidateOptionalText(input.CurrentAddress, "Input.CurrentAddress", "Địa chỉ hiện tại");
        ValidateOptionalText(input.PermanentAddress, "Input.PermanentAddress", "Địa chỉ thường trú");
        ValidateOptionalText(input.Hometown, "Input.Hometown", "Nguyên quán");
        ValidateOptionalText(input.PlaceOfBirth, "Input.PlaceOfBirth", "Nơi sinh");

        var normalizedPhone = NormalizePhoneNumber(input.PhoneNumber);

        if (!IsValidVietnamPhoneNumber(normalizedPhone))
        {
            ModelState.AddModelError("Input.PhoneNumber", "Số điện thoại phải gồm 10 đến 12 số và bắt đầu bằng 0.");
        }

        if (input.AvatarFile != null)
        {
            ValidateAvatarFile(input.AvatarFile);
        }

        var teacherCodeExists = await Context.Teachers
            .AsNoTracking()
            .AnyAsync(
                t => t.TeacherCode == input.TeacherCode &&
                    (excludedTeacherId == null || t.TeacherId != excludedTeacherId.Value),
                cancellationToken);

        if (teacherCodeExists)
        {
            ModelState.AddModelError("Input.TeacherCode", "Mã giáo viên đã tồn tại.");
        }

        var phoneExists = await Context.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.NormalizedPhoneNumber == normalizedPhone &&
                    (excludedUserId == null || u.UserId != excludedUserId.Value),
                cancellationToken);

        if (phoneExists)
        {
            ModelState.AddModelError("Input.PhoneNumber", "Số điện thoại đã tồn tại.");
        }

        var identityExists = await Context.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.IdentityNumber == input.IdentityNumber &&
                    (excludedUserId == null || u.UserId != excludedUserId.Value),
                cancellationToken);

        if (identityExists)
        {
            ModelState.AddModelError("Input.IdentityNumber", "CMND/CCCD đã tồn tại.");
        }

        if (!string.IsNullOrWhiteSpace(input.Email))
        {
            var emailExists = await Context.Users
                .AsNoTracking()
                .AnyAsync(
                    u => u.Email == input.Email &&
                        (excludedUserId == null || u.UserId != excludedUserId.Value),
                    cancellationToken);

            if (emailExists)
            {
                ModelState.AddModelError("Input.Email", "Email đã tồn tại.");
            }
        }
    }

    protected static string NormalizePhoneNumber(string phoneNumber)
    {
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        return digits.StartsWith("84", StringComparison.Ordinal) && digits.Length > 9
            ? $"0{digits[2..]}"
            : digits;
    }

    protected async Task<string?> SaveAvatarAsync(
        IFormFile? avatarFile,
        string teacherCode,
        CancellationToken cancellationToken)
    {
        if (avatarFile == null || avatarFile.Length == 0)
        {
            return null;
        }

        var uploadDirectory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "uploads",
            "teachers");

        Directory.CreateDirectory(uploadDirectory);

        var extension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
        var fileName = $"{teacherCode.ToLowerInvariant()}-{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadDirectory, fileName);

        await using var stream = System.IO.File.Create(fullPath);
        await avatarFile.CopyToAsync(stream, cancellationToken);

        return $"/uploads/teachers/{fileName}";
    }

    protected static void DeleteUploadedAvatar(string? avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
        {
            return;
        }

        var relativePath = avatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }

    private static bool IsValidTeacherCode(string teacherCode)
    {
        return !string.IsNullOrWhiteSpace(teacherCode) &&
            teacherCode.Length <= 30 &&
            teacherCode.All(c => char.IsLetterOrDigit(c) || c == '-');
    }

    private static bool IsValidFullName(string fullName)
    {
        return fullName.Length >= 2 &&
            Regex.IsMatch(fullName, @"^[\p{L}\s'.-]+$") &&
            fullName.Any(char.IsLetter) &&
            !fullName.Any(char.IsDigit);
    }

    private static bool IsValidIdentityNumber(string value)
    {
        return value.All(char.IsDigit) && value.Length is 9 or 12;
    }

    private static bool IsValidVietnamPhoneNumber(string normalizedPhone)
    {
        return normalizedPhone.Length >= 10 &&
            normalizedPhone.Length <= 12 &&
            normalizedPhone.StartsWith('0') &&
            normalizedPhone.All(char.IsDigit);
    }

    private void ValidateOptionalText(
        string? value,
        string fieldName,
        string displayName)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Any(char.IsControl))
        {
            ModelState.AddModelError(fieldName, $"{displayName} không hợp lệ.");
        }
    }

    private void ValidateAvatarFile(IFormFile avatarFile)
    {
        const long maxFileSize = 2 * 1024 * 1024;
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };
        var allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        if (avatarFile.Length == 0)
        {
            ModelState.AddModelError("Input.AvatarFile", "Ảnh đại diện không hợp lệ.");
            return;
        }

        if (avatarFile.Length > maxFileSize)
        {
            ModelState.AddModelError("Input.AvatarFile", "Ảnh đại diện tối đa 2MB.");
            return;
        }

        var extension = Path.GetExtension(avatarFile.FileName);

        if (!allowedExtensions.Contains(extension) || !allowedContentTypes.Contains(avatarFile.ContentType))
        {
            ModelState.AddModelError("Input.AvatarFile", "Ảnh đại diện chỉ nhận JPG, PNG hoặc WEBP.");
        }
    }

    private static DateOnly GetVietnamToday()
    {
        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

        return DateOnly.FromDateTime(vietnamNow);
    }

}
