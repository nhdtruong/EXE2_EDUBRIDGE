using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EduBridge.Pages.AdminTeachers;

public sealed class TeacherProfileInput
{
    [Required(ErrorMessage = "Vui lòng nhập mã giáo viên.")]
    [MaxLength(30, ErrorMessage = "Mã giáo viên tối đa 30 ký tự.")]
    public string TeacherCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [MaxLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn ngày sinh.")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
    [MaxLength(10, ErrorMessage = "Giới tính tối đa 10 ký tự.")]
    public string Gender { get; set; } = "Nam";

    [MaxLength(50, ErrorMessage = "Dân tộc tối đa 50 ký tự.")]
    public string? Ethnicity { get; set; }

    [MaxLength(50, ErrorMessage = "Tôn giáo tối đa 50 ký tự.")]
    public string? Religion { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập CMND/CCCD.")]
    [MaxLength(20, ErrorMessage = "CMND/CCCD tối đa 20 ký tự.")]
    public string IdentityNumber { get; set; } = string.Empty;

    public DateOnly? IdentityIssuedDate { get; set; }

    [MaxLength(150, ErrorMessage = "Nơi cấp tối đa 150 ký tự.")]
    public string? IdentityIssuedPlace { get; set; }

    [MaxLength(255, ErrorMessage = "Địa chỉ hiện tại tối đa 255 ký tự.")]
    public string? CurrentAddress { get; set; }

    [MaxLength(255, ErrorMessage = "Địa chỉ thường trú tối đa 255 ký tự.")]
    public string? PermanentAddress { get; set; }

    [MaxLength(150, ErrorMessage = "Nguyên quán tối đa 150 ký tự.")]
    public string? Hometown { get; set; }

    [MaxLength(150, ErrorMessage = "Nơi sinh tối đa 150 ký tự.")]
    public string? PlaceOfBirth { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [MaxLength(150, ErrorMessage = "Email tối đa 150 ký tự.")]
    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    public IFormFile? AvatarFile { get; set; }

    public bool RemoveAvatar { get; set; }

    public void Normalize()
    {
        TeacherCode = TeacherCode.Trim().ToUpperInvariant();
        FullName = NormalizeSpaces(FullName);
        Gender = NormalizeOptional(Gender) ?? "Nam";
        Ethnicity = NormalizeOptional(Ethnicity);
        Religion = NormalizeOptional(Religion);
        IdentityNumber = new string(IdentityNumber.Where(char.IsDigit).ToArray());
        IdentityIssuedPlace = NormalizeOptional(IdentityIssuedPlace);
        CurrentAddress = NormalizeOptional(CurrentAddress);
        PermanentAddress = NormalizeOptional(PermanentAddress);
        Hometown = NormalizeOptional(Hometown);
        PlaceOfBirth = NormalizeOptional(PlaceOfBirth);
        PhoneNumber = PhoneNumber.Trim();
        Email = string.IsNullOrWhiteSpace(Email)
            ? null
            : Email.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = NormalizeSpaces(value);

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }

    private static string NormalizeSpaces(string value)
    {
        return Regex.Replace(value.Trim(), @"\s+", " ");
    }
}
