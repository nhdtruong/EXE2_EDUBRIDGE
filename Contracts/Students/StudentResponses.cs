using System;
using System.Collections.Generic;

namespace EduBridge.Contracts.Students;

public class StudentResponse
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Ethnicity { get; set; }
    public string? Religion { get; set; }
    public string? IdentityNumber { get; set; }
    public DateOnly? IdentityIssuedDate { get; set; }
    public string? IdentityIssuedPlace { get; set; }
    public string? CurrentAddress { get; set; }
    public string? PermanentAddress { get; set; }
    public string? Hometown { get; set; }
    public string? PlaceOfBirth { get; set; }

    public int ParentUserId { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string? ParentPhone { get; set; }
    public string? ParentEmail { get; set; }

    public List<StudentClassResponse> CurrentClasses { get; set; } = new();
}

public class StudentClassResponse
{
    public int ClassId { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
}

public class StudentPagedResponse
{
    public List<StudentResponse> Data { get; set; } = new();
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => TotalItems == 0 ? 1 : (int)Math.Ceiling((double)TotalItems / PageSize);
}

public class ParentSearchResultResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class StudentMutationResponse
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public StudentMutationResponse(int studentId, string studentCode, string status)
    {
        StudentId = studentId;
        StudentCode = studentCode;
        Status = status;
    }
}
