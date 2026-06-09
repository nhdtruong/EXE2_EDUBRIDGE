using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class Center
{
    public int CenterId { get; set; }

    public int OwnerUserId { get; set; }

    public string CenterName { get; set; } = null!;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? SettingsJson { get; set; }

    public virtual ICollection<CenterUser> CenterUsers { get; set; } = new List<CenterUser>();

    public virtual ICollection<ClassCodeCounter> ClassCodeCounters { get; set; } = new List<ClassCodeCounter>();

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    public virtual ICollection<InvoiceCodeCounter> InvoiceCodeCounters { get; set; } = new List<InvoiceCodeCounter>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual User OwnerUser { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<StudyShift> StudyShifts { get; set; } = new List<StudyShift>();

    public virtual TeacherCodeCounter? TeacherCodeCounter { get; set; }

    public virtual ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
}
