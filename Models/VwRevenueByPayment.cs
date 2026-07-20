using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class VwRevenueByPayment
{
    public int CenterId { get; set; }

    public DateOnly? PaidDate { get; set; }

    public int? PaidYear { get; set; }

    public int? PaidMonth { get; set; }

    public decimal? RevenueAmount { get; set; }
}
