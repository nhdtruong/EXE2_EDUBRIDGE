using System;

namespace EduBridge.Contracts.Finance;

public sealed record IssueReceiptRequest(
    int PaymentId
);

public sealed record ReceiptResponse(
    int ReceiptId,
    string ReceiptNumber,
    int PaymentId,
    string StudentName,
    string ClassName,
    string CourseName,
    decimal Amount,
    string PaymentMethod,
    DateTime IssuedAt,
    string IssuedByUserName,
    string Status,
    string? VoidReason
);

public sealed record ReceiptPrintResponse(
    int ReceiptId,
    string ReceiptNumber,
    DateTime IssuedAt,
    string PaymentMethod,
    string CenterName,
    string CenterAddress,
    string? CenterPhoneNumber,
    string? CenterEmail,
    string InvoiceCode,
    string StudentName,
    string StudentCode,
    string ClassName,
    string CourseName,
    string? ParentName,
    string? ParentPhoneNumber,
    string? Description,
    decimal InvoiceAmount,
    decimal DiscountAmount,
    decimal AmountPaid,
    decimal TotalPaidBeforeThis,
    decimal FinalInvoiceAmount,
    string AmountInWords,
    string IssuedByUserName,
    string? TransactionReference,
    string Status
);

public sealed record ReceiptFilterRequest(
    int? PaymentId = null,
    string? Keyword = null,
    string? Status = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    int PageNumber = 1,
    int PageSize = 10
);
