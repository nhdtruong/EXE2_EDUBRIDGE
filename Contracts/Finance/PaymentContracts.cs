using System;
using System.Collections.Generic;

namespace EduBridge.Contracts.Finance;

public sealed record CreatePaymentRequest(
    int InvoiceId,
    decimal Amount,
    string PaymentMethod,
    string? TransactionReference
);

public sealed record PaymentFilterRequest(
    int? InvoiceId = null,
    string? PaymentMethod = null,
    string? Status = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    int PageNumber = 1,
    int PageSize = 10
);

public sealed record PaymentResponse(
    int PaymentId,
    int InvoiceId,
    string InvoiceCode,
    decimal Amount,
    string PaymentMethod,
    string? TransactionReference,
    string Status,
    DateTime CreatedAt,
    string ReceivedByUserName
);
