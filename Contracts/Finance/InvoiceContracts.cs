using System;
using System.Collections.Generic;

namespace EduBridge.Contracts.Finance;

public sealed record Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string Message { get; }
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    private Result(bool isSuccess, T? value, string message, IReadOnlyDictionary<string, string[]> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Message = message;
        Errors = errors;
    }

    public static Result<T> Success(T value, string message = "Success") => 
        new(true, value, message, new Dictionary<string, string[]>());

    public static Result<T> Failure(string message, IReadOnlyDictionary<string, string[]>? errors = null) => 
        new(false, default, message, errors ?? new Dictionary<string, string[]>());
}

public sealed record Result
{
    public bool IsSuccess { get; }
    public string Message { get; }
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    private Result(bool isSuccess, string message, IReadOnlyDictionary<string, string[]> errors)
    {
        IsSuccess = isSuccess;
        Message = message;
        Errors = errors;
    }

    public static Result Success(string message = "Success") => 
        new(true, message, new Dictionary<string, string[]>());

    public static Result Failure(string message, IReadOnlyDictionary<string, string[]>? errors = null) => 
        new(false, message, errors ?? new Dictionary<string, string[]>());
}

public sealed record PagedList<T>(
    IReadOnlyList<T> Items,
    int TotalItems,
    int PageNumber,
    int PageSize,
    int TotalPages
);

public sealed record CreateInvoiceRequest(
    int StudentId,
    int ClassId,
    decimal? Amount,
    decimal? DiscountAmount,
    string? DiscountNote,
    DateOnly? DueDate,
    string? Description
);

public sealed record InvoiceResponse(
    int InvoiceId,
    string InvoiceCode,
    int StudentId,
    string StudentName,
    string StudentCode,
    int ClassId,
    string ClassName,
    int CourseId,
    string CourseName,
    decimal Amount,
    decimal DiscountAmount,
    decimal FinalAmount,
    DateOnly? DueDate,
    string Status,
    decimal PaidAmount,
    decimal RemainingAmount,
    DateTime CreatedAt,
    string CreatedByUserName,
    int? LatestReceiptId = null
);

public sealed record InvoiceDetailResponse(
    int InvoiceId,
    string InvoiceCode,
    int StudentId,
    string StudentName,
    string StudentCode,
    int ClassId,
    string ClassName,
    int CourseId,
    string CourseName,
    decimal Amount,
    decimal DiscountAmount,
    decimal FinalAmount,
    DateOnly? DueDate,
    string Status,
    decimal PaidAmount,
    decimal RemainingAmount,
    DateTime CreatedAt,
    string CreatedByUserName,
    string? Description,
    string? DiscountNote,
    IReadOnlyList<PaymentDto> Payments
);

public sealed record PaymentDto(
    int PaymentId,
    decimal Amount,
    string PaymentMethod,
    string? TransactionReference,
    string Status,
    DateTime CreatedAt,
    string ReceivedByUserName
);

public sealed record InvoiceFilterRequest(
    int? StudentId = null,
    int? ClassId = null,
    string? Status = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    string? InvoiceCode = null,
    string? StudentName = null,
    int PageNumber = 1,
    int PageSize = 10,
    string? SortBy = "CreatedAt",
    string? SortDirection = "DESC"
);

public sealed record DebtFilterRequest(
    int? ClassId = null,
    int PageNumber = 1,
    int PageSize = 10
);

public sealed record StudentDebtResponse(
    int StudentId,
    string StudentName,
    string StudentCode,
    decimal TotalDebt,
    int InvoiceCount,
    DateOnly? OldestDueDate,
    int OverdueDays
);
