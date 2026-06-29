using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class User
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }

    public bool EmailConfirmed { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public string? NormalizedPhoneNumber { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Address { get; set; }

    public string? Ethnicity { get; set; }

    public string? Religion { get; set; }

    public string? IdentityNumber { get; set; }

    public DateOnly? IdentityIssuedDate { get; set; }

    public string? IdentityIssuedPlace { get; set; }

    public string? CurrentAddress { get; set; }

    public string? PermanentAddress { get; set; }

    public string? Hometown { get; set; }

    public string? PlaceOfBirth { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    public virtual ICollection<CenterUser> CenterUsers { get; set; } = new List<CenterUser>();

    public virtual ICollection<Center> Centers { get; set; } = new List<Center>();

    public virtual ICollection<Class> ClassClosedByUsers { get; set; } = new List<Class>();

    public virtual ICollection<Class> ClassDeletedByUsers { get; set; } = new List<Class>();

    public virtual ICollection<Class> ClassUpdatedByUsers { get; set; } = new List<Class>();

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    public virtual ICollection<EnrollmentHistory> EnrollmentHistories { get; set; } = new List<EnrollmentHistory>();

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Message> MessageReceiverUsers { get; set; } = new List<Message>();

    public virtual ICollection<Message> MessageSenderUsers { get; set; } = new List<Message>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Receipt> ReceiptIssuedByUsers { get; set; } = new List<Receipt>();

    public virtual ICollection<Receipt> ReceiptVoidedByUsers { get; set; } = new List<Receipt>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<StudyShift> StudyShifts { get; set; } = new List<StudyShift>();

    public virtual ICollection<SystemAuditLog> SystemAuditLogs { get; set; } = new List<SystemAuditLog>();

    public virtual Teacher? Teacher { get; set; }
}
