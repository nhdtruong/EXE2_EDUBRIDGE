using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int InvoiceId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaidAt { get; set; }

    public string? PaymentMethod { get; set; }

    public string? Note { get; set; }

    public int CenterId { get; set; }

    public int ReceivedByUserId { get; set; }

    public string Status { get; set; } = null!;

    public string? TransactionReference { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Center Center { get; set; } = null!;

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();

    public virtual User ReceivedByUser { get; set; } = null!;
}
