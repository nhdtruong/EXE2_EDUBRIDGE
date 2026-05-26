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

    public virtual Invoice Invoice { get; set; } = null!;
}
