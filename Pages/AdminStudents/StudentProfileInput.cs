using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EduBridge.Pages.AdminStudents;

public sealed class StudentProfileInput
{
    public int StudentId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã học sinh.")]
    [StringLength(30, MinimumLength = 2, ErrorMessage = "Mã học sinh phải dài 2-30 ký tự.")]
    public string StudentCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ và tên học sinh.")]
    [StringLength(120, ErrorMessage = "Họ và tên học sinh tối đa 120 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn ngày sinh.")]
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

    [StringLength(20, ErrorMessage = "Số điện thoại học sinh tối đa 20 ký tự.")]
    public string? StudentPhoneNumber { get; set; }

    [EmailAddress(ErrorMessage = "Email học sinh không hợp lệ.")]
    [StringLength(150, ErrorMessage = "Email học sinh tối đa 150 ký tự.")]
    public string? StudentEmail { get; set; }

    public bool IsActive { get; set; } = true;

    public int? ParentUserId { get; set; }

    [StringLength(120, ErrorMessage = "Họ và tên phụ huynh tối đa 120 ký tự.")]
    public string ParentFullName { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Số điện thoại phụ huynh tối đa 20 ký tự.")]
    public string ParentPhoneNumber { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email phụ huynh không hợp lệ.")]
    [StringLength(150, ErrorMessage = "Email phụ huynh tối đa 150 ký tự.")]
    public string? ParentEmail { get; set; }

    public string? AvatarUrl { get; set; }

    public bool RemoveAvatar { get; set; }

    public IFormFile? AvatarFile { get; set; }

    public void Normalize()
    {
        StudentCode = NormalizeRequired(StudentCode).ToUpperInvariant();
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
        StudentPhoneNumber = NormalizeOptional(StudentPhoneNumber);
        StudentEmail = NormalizeOptional(StudentEmail)?.ToLowerInvariant();
        ParentFullName = NormalizeRequired(ParentFullName);
        ParentPhoneNumber = NormalizeRequired(ParentPhoneNumber);
        ParentEmail = NormalizeOptional(ParentEmail)?.ToLowerInvariant();
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
