using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class Receipt
{
    public int ReceiptId { get; set; }

    public string ReceiptNumber { get; set; } = null!;

    public int PaymentId { get; set; }

    public int CenterId { get; set; }

    public string StudentName { get; set; } = null!;

    public string ClassName { get; set; } = null!;

    public string CourseName { get; set; } = null!;

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public DateTime IssuedAt { get; set; }

    public int IssuedByUserId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? VoidedAt { get; set; }

    public int? VoidedByUserId { get; set; }

    public string? VoidReason { get; set; }

    public virtual Center Center { get; set; } = null!;

    public virtual User IssuedByUser { get; set; } = null!;

    public virtual Payment Payment { get; set; } = null!;

    public virtual User? VoidedByUser { get; set; }
}
