using System.ComponentModel.DataAnnotations;

namespace EduBridge.Contracts.Parents;

public sealed class ParentQuery
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Status { get; set; }
    public bool? HasChildren { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class SaveParentRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(10)]
    public string? Gender { get; set; } = "Nam";

    [StringLength(20)]
    public string? IdentityNumber { get; set; }

    public DateOnly? IdentityIssuedDate { get; set; }

    [StringLength(255)]
    public string? IdentityIssuedPlace { get; set; }

    [StringLength(100)]
    public string? Ethnicity { get; set; }

    [StringLength(100)]
    public string? Religion { get; set; }

    [StringLength(255)]
    public string? CurrentAddress { get; set; }

    [StringLength(255)]
    public string? PermanentAddress { get; set; }

    [StringLength(255)]
    public string? Hometown { get; set; }

    [StringLength(255)]
    public string? PlaceOfBirth { get; set; }

    public string Status { get; set; } = "Active";
}

public sealed record ParentListItemResponse(
    int UserId,
    string FullName,
    string? PhoneNumber,
    string? Email,
    string Status,
    DateTime CreatedAt,
    int ChildrenCount,
    IReadOnlyList<string> ChildrenNames);

public sealed record ParentDetailResponse(
    int UserId,
    string FullName,
    string? PhoneNumber,
    string? Email,
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
    DateTime CreatedAt,
    IReadOnlyList<ParentChildResponse> Children);

public sealed record ParentChildResponse(
    int StudentId,
    string StudentCode,
    string FullName,
    DateOnly? DateOfBirth,
    string? Gender,
    string Status);

public sealed record ParentPagedResponse(
    IReadOnlyList<ParentListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record ParentMutationResponse(int UserId, bool CreatedNewUser, string Status);

public sealed record LinkableStudentResponse(int StudentId, string StudentCode, string FullName, string CurrentParentName);

public sealed record ResetParentPasswordResponse(int UserId, string TemporaryPassword);
