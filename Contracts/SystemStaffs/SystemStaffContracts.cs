using System.ComponentModel.DataAnnotations;

namespace EduBridge.Contracts.SystemStaffs;

public sealed class SystemStaffQuery
{
    public string? NameCodeKeyword { get; set; }
    public string? EmailPhoneKeyword { get; set; }
    public string? Status { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class SaveSystemStaffRequest
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mã nhân sự.")]
    [StringLength(50)]
    public string StaffCode { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(150)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập ngày sinh.")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
    public string Gender { get; set; } = "Nam";

    [Required(ErrorMessage = "Vui lòng nhập số CMND/CCCD.")]
    [StringLength(20)]
    public string IdentityNumber { get; set; } = string.Empty;

    public DateOnly? IdentityIssuedDate { get; set; }

    [StringLength(150)]
    public string? IdentityIssuedPlace { get; set; }

    [StringLength(50)]
    public string? Ethnicity { get; set; }

    [StringLength(50)]
    public string? Religion { get; set; }

    [StringLength(255)]
    public string? CurrentAddress { get; set; }

    [StringLength(255)]
    public string? PermanentAddress { get; set; }

    [StringLength(150)]
    public string? Hometown { get; set; }

    [StringLength(150)]
    public string? PlaceOfBirth { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed record SystemStaffListItemResponse(
    int UserId,
    string FullName,
    string Role,
    string? StaffCode,
    string? PhoneNumber,
    string? Email,
    string? AvatarUrl,
    string Status,
    DateTime CreatedAt);

public sealed record SystemStaffDetailResponse(
    int UserId,
    string FullName,
    string Role,
    string? StaffCode,
    string? PhoneNumber,
    string? Email,
    string? AvatarUrl,
    DateOnly? DateOfBirth,
    string? Gender,
    string? IdentityNumber,
    DateOnly? IdentityIssuedDate,
    string? IdentityIssuedPlace,
    string? Ethnicity,
    string? Religion,
    string? CurrentAddress,
    string? PermanentAddress,
    string? Hometown,
    string? PlaceOfBirth,
    string Status,
    DateTime CreatedAt);

public sealed record SystemStaffPagedResponse(
    IReadOnlyList<SystemStaffListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record SystemStaffMutationResponse(int UserId, bool CreatedNewUser, string Status);

public sealed record ResetSystemStaffPasswordResponse(int UserId, string TemporaryPassword);
