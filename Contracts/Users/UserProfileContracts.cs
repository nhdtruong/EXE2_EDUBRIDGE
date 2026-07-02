using System.ComponentModel.DataAnnotations;
using System;

namespace EduBridge.Contracts.Users;

public sealed class UserProfileResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? IdentityNumber { get; set; }
    public DateOnly? IdentityIssuedDate { get; set; }
    public string? IdentityIssuedPlace { get; set; }
    public string? CurrentAddress { get; set; }
    public string? PermanentAddress { get; set; }
    public string? Hometown { get; set; }
    public string? PlaceOfBirth { get; set; }
    public string? Ethnicity { get; set; }
    public string? Religion { get; set; }
}

public sealed class UpdateProfileRequest
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(256)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(20)]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn ngày sinh.")]
    public DateOnly DateOfBirth { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
    [StringLength(10)]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số CMND/CCCD.")]
    [StringLength(20)]
    public string IdentityNumber { get; set; } = string.Empty;

    public DateOnly? IdentityIssuedDate { get; set; }

    [StringLength(200)]
    public string? IdentityIssuedPlace { get; set; }

    [StringLength(500)]
    public string? CurrentAddress { get; set; }

    [StringLength(500)]
    public string? PermanentAddress { get; set; }

    [StringLength(200)]
    public string? Hometown { get; set; }

    [StringLength(200)]
    public string? PlaceOfBirth { get; set; }

    [StringLength(50)]
    public string? Ethnicity { get; set; }

    [StringLength(50)]
    public string? Religion { get; set; }

    [StringLength(500)]
    public string? AvatarUrl { get; set; }
}