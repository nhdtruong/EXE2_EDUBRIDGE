using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class Student
{
    public int StudentId { get; set; }

    public int ParentUserId { get; set; }

    public int CenterId { get; set; }

    public string StudentCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Address { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? NormalizedPhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    public string? Ethnicity { get; set; }

    public string? Religion { get; set; }

    public string? IdentityNumber { get; set; }

    public DateOnly? IdentityIssuedDate { get; set; }

    public string? IdentityIssuedPlace { get; set; }

    public string? PermanentAddress { get; set; }

    public string? Hometown { get; set; }

    public string? PlaceOfBirth { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Center Center { get; set; } = null!;

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual User ParentUser { get; set; } = null!;

    public virtual ICollection<HomeworkSubmission> HomeworkSubmissions { get; set; } = new List<HomeworkSubmission>();
}
