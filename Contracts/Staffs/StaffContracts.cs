using System.ComponentModel.DataAnnotations;

namespace EduBridge.Contracts.Staffs;

public sealed class StaffQuery
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public string? Role { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class SaveStaffRequest
{
    [Required(ErrorMessage = "Vui lòng chọn ít nhất 1 vai trò.")]
    public List<string> Roles { get; set; } = new() { "TEACHER" };

    [Required(ErrorMessage = "Vui lòng nhập mã nhân sự.")]
    [StringLength(30)]
    public string StaffCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(150)]
    public string? Email { get; set; }
    
    [StringLength(100)]
    public string? Specialization { get; set; }
    
    public int? ExperienceYears { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(10)]
    public string Gender { get; set; } = "Nam";

    [StringLength(50)]
    public string? Ethnicity { get; set; }

    [StringLength(50)]
    public string? Religion { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập CMND/CCCD.")]
    [StringLength(20)]
    public string IdentityNumber { get; set; } = string.Empty;

    public DateOnly? IdentityIssuedDate { get; set; }

    [StringLength(150)]
    public string? IdentityIssuedPlace { get; set; }

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

public sealed record StaffListItemResponse(
    int UserId,
    string StaffCode,
    string FullName,
    List<string> Roles,
    string? PhoneNumber,
    string? Email,
    string? AvatarUrl,
    string? Specialization,
    int ClassCount,
    int StudentCount,
    string Status,
    DateTime CreatedAt);

public sealed record StaffDetailResponse(
    int UserId,
    string StaffCode,
    string FullName,
    List<string> Roles,
    string? PhoneNumber,
    string? Email,
    string? AvatarUrl,
    string? Specialization,
    int ExperienceYears,
    DateOnly? DateOfBirth,
    string Gender,
    string? Ethnicity,
    string? Religion,
    string IdentityNumber,
    DateOnly? IdentityIssuedDate,
    string? IdentityIssuedPlace,
    string? CurrentAddress,
    string? PermanentAddress,
    string? Hometown,
    string? PlaceOfBirth,
    string Status,
    DateTime CreatedAt);

public sealed record StaffPagedResponse(
    IReadOnlyList<StaffListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record StaffMutationResponse(int UserId, bool CreatedNewUser, string Status);

public sealed record ResetStaffPasswordResponse(int UserId, string TemporaryPassword);
