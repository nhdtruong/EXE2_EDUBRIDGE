using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EduBridge.Pages.AdminStudents;

public sealed class ParentProfileInput
{
    public int ParentUserId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ và tên phụ huynh.")]
    [StringLength(120, ErrorMessage = "Họ và tên phụ huynh tối đa 120 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(10, ErrorMessage = "Giới tính không hợp lệ.")]
    public string? Gender { get; set; } = "Nam";

    [StringLength(50, ErrorMessage = "Dân tộc tối đa 50 ký tự.")]
    public string? Ethnicity { get; set; }

    [StringLength(50, ErrorMessage = "Tôn giáo tối đa 50 ký tự.")]
    public string? Religion { get; set; }

    [StringLength(20, ErrorMessage = "CMND/CCCD tối đa 20 ký tự.")]
    public string? IdentityNumber { get; set; }

    public DateOnly? IdentityIssuedDate { get; set; }

    [StringLength(150, ErrorMessage = "Nơi cấp tối đa 150 ký tự.")]
    public string? IdentityIssuedPlace { get; set; }

    [StringLength(255, ErrorMessage = "Địa chỉ hiện tại tối đa 255 ký tự.")]
    public string? CurrentAddress { get; set; }

    [StringLength(255, ErrorMessage = "Địa chỉ thường trú tối đa 255 ký tự.")]
    public string? PermanentAddress { get; set; }

    [StringLength(150, ErrorMessage = "Nguyên quán tối đa 150 ký tự.")]
    public string? Hometown { get; set; }

    [StringLength(150, ErrorMessage = "Nơi sinh tối đa 150 ký tự.")]
    public string? PlaceOfBirth { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại phụ huynh.")]
    [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(150, ErrorMessage = "Email tối đa 150 ký tự.")]
    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    public void Normalize()
    {
        FullName = NormalizeRequired(FullName);
        Gender = NormalizeOptional(Gender);
        Ethnicity = NormalizeOptional(Ethnicity);
        Religion = NormalizeOptional(Religion);
        IdentityNumber = NormalizeOptional(IdentityNumber);
        IdentityIssuedPlace = NormalizeOptional(IdentityIssuedPlace);
        CurrentAddress = NormalizeOptional(CurrentAddress);
        PermanentAddress = NormalizeOptional(PermanentAddress);
        Hometown = NormalizeOptional(Hometown);
        PlaceOfBirth = NormalizeOptional(PlaceOfBirth);
        PhoneNumber = NormalizeRequired(PhoneNumber);
        Email = NormalizeOptional(Email)?.ToLowerInvariant();
    }

    private static string NormalizeRequired(string? value)
    {
        return Regex.Replace(value?.Trim() ?? string.Empty, @"\s+", " ");
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Regex.Replace(value.Trim(), @"\s+", " ");
    }
}
