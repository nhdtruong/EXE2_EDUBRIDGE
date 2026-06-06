using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace EduBridge.Contracts.Students;

public class SaveStudentRequest
{
    [Required]
    public string StudentCode { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Ethnicity { get; set; }

    public string? Religion { get; set; }

    public string? IdentityNumber { get; set; }

    public DateOnly? IdentityIssuedDate { get; set; }

    public string? IdentityIssuedPlace { get; set; }

    public string? CurrentAddress { get; set; }

    public string? PermanentAddress { get; set; }

    public string? Hometown { get; set; }

    public string? PlaceOfBirth { get; set; }

    public string? StudentPhoneNumber { get; set; }

    public string? StudentEmail { get; set; }

    public int? ParentUserId { get; set; }

    public string? ParentFullName { get; set; }

    public string? ParentPhoneNumber { get; set; }

    public string? ParentEmail { get; set; }

    public IFormFile? AvatarFile { get; set; }
    
    public void Normalize()
    {
        StudentCode = StudentCode?.Trim() ?? string.Empty;
        FullName = FullName?.Trim() ?? string.Empty;
        Gender = string.IsNullOrWhiteSpace(Gender) ? null : Gender.Trim();
        Ethnicity = string.IsNullOrWhiteSpace(Ethnicity) ? null : Ethnicity.Trim();
        Religion = string.IsNullOrWhiteSpace(Religion) ? null : Religion.Trim();
        IdentityNumber = string.IsNullOrWhiteSpace(IdentityNumber) ? null : IdentityNumber.Trim();
        IdentityIssuedPlace = string.IsNullOrWhiteSpace(IdentityIssuedPlace) ? null : IdentityIssuedPlace.Trim();
        CurrentAddress = string.IsNullOrWhiteSpace(CurrentAddress) ? null : CurrentAddress.Trim();
        PermanentAddress = string.IsNullOrWhiteSpace(PermanentAddress) ? null : PermanentAddress.Trim();
        Hometown = string.IsNullOrWhiteSpace(Hometown) ? null : Hometown.Trim();
        PlaceOfBirth = string.IsNullOrWhiteSpace(PlaceOfBirth) ? null : PlaceOfBirth.Trim();
        StudentPhoneNumber = string.IsNullOrWhiteSpace(StudentPhoneNumber) ? null : StudentPhoneNumber.Trim();
        StudentEmail = string.IsNullOrWhiteSpace(StudentEmail) ? null : StudentEmail.Trim();

        ParentFullName = string.IsNullOrWhiteSpace(ParentFullName) ? null : ParentFullName.Trim();
        ParentPhoneNumber = string.IsNullOrWhiteSpace(ParentPhoneNumber) ? null : ParentPhoneNumber.Trim();
        ParentEmail = string.IsNullOrWhiteSpace(ParentEmail) ? null : ParentEmail.Trim();
    }
}

public class UpdateStudentRequest
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Ethnicity { get; set; }

    public string? Religion { get; set; }

    public string? IdentityNumber { get; set; }

    public DateOnly? IdentityIssuedDate { get; set; }

    public string? IdentityIssuedPlace { get; set; }

    public string? CurrentAddress { get; set; }

    public string? PermanentAddress { get; set; }

    public string? Hometown { get; set; }

    public string? PlaceOfBirth { get; set; }

    public string? StudentPhoneNumber { get; set; }

    public string? StudentEmail { get; set; }

    public IFormFile? AvatarFile { get; set; }
    public bool RemoveAvatar { get; set; }

    public void Normalize()
    {
        FullName = FullName?.Trim() ?? string.Empty;
        Gender = string.IsNullOrWhiteSpace(Gender) ? null : Gender.Trim();
        Ethnicity = string.IsNullOrWhiteSpace(Ethnicity) ? null : Ethnicity.Trim();
        Religion = string.IsNullOrWhiteSpace(Religion) ? null : Religion.Trim();
        IdentityNumber = string.IsNullOrWhiteSpace(IdentityNumber) ? null : IdentityNumber.Trim();
        IdentityIssuedPlace = string.IsNullOrWhiteSpace(IdentityIssuedPlace) ? null : IdentityIssuedPlace.Trim();
        CurrentAddress = string.IsNullOrWhiteSpace(CurrentAddress) ? null : CurrentAddress.Trim();
        PermanentAddress = string.IsNullOrWhiteSpace(PermanentAddress) ? null : PermanentAddress.Trim();
        Hometown = string.IsNullOrWhiteSpace(Hometown) ? null : Hometown.Trim();
        PlaceOfBirth = string.IsNullOrWhiteSpace(PlaceOfBirth) ? null : PlaceOfBirth.Trim();
        StudentPhoneNumber = string.IsNullOrWhiteSpace(StudentPhoneNumber) ? null : StudentPhoneNumber.Trim();
        StudentEmail = string.IsNullOrWhiteSpace(StudentEmail) ? null : StudentEmail.Trim();
    }
}

public class UpdateStudentParentRequest
{
    [Required]
    public int ParentUserId { get; set; }
}

public class UpdateStudentStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}
