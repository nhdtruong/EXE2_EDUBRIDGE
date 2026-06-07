using System.ComponentModel.DataAnnotations;

namespace EduBridge.Contracts.Teachers;

public sealed class TeacherQuery
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class SaveTeacherRequest
{
    [Required(ErrorMessage = "Vui lòng nhập mã giáo viên.")]
    [StringLength(30)]
    public string TeacherCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(150)]
    public string? Email { get; set; }

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

public sealed record TeacherListItemResponse(
    int UserId,
    string TeacherCode,
    string FullName,
    string? PhoneNumber,
    string? Email,
    string? AvatarUrl,
    int ClassCount,
    int StudentCount,
    string Status,
    DateTime CreatedAt);

public sealed record TeacherDetailResponse(
    int UserId,
    string TeacherCode,
    string FullName,
    string? PhoneNumber,
    string? Email,
    string? AvatarUrl,
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

public sealed record TeacherPagedResponse(
    IReadOnlyList<TeacherListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record TeacherMutationResponse(int UserId, bool CreatedNewUser, string Status);

public sealed record ResetTeacherPasswordResponse(int UserId, string TemporaryPassword);
