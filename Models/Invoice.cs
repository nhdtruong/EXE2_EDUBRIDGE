using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public int StudentId { get; set; }

    public int ClassId { get; set; }

    public decimal Amount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal? FinalAmount { get; set; }

    public DateOnly? DueDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public int CenterId { get; set; }

    public string InvoiceCode { get; set; } = null!;

    public int? EnrollmentId { get; set; }

    public string? Description { get; set; }

    public string? DiscountNote { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Center Center { get; set; } = null!;

    public virtual Class Class { get; set; } = null!;

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual Enrollment? Enrollment { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Student Student { get; set; } = null!;
}
