using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Contracts.Finance;
using Microsoft.Extensions.Logging;

namespace EduBridge.Services.Finance;

public sealed class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(AppDbContext context, IInvoiceService invoiceService, ILogger<PaymentService> logger)
    {
        _context = context;
        _invoiceService = invoiceService;
        _logger = logger;
    }

    public async Task<Result<PaymentResponse>> CreatePaymentAsync(CreatePaymentRequest request, int centerId, int userId, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0) return Result<PaymentResponse>.Failure("Số tiền thanh toán phải lớn hơn 0.");

        var invoice = await _context.Invoices
            .Include(i => i.Payments)
            .Include(i => i.Student)
            .Include(i => i.Class).ThenInclude(c => c.Course)
            .FirstOrDefaultAsync(i => i.InvoiceId == request.InvoiceId && i.CenterId == centerId, cancellationToken);

        if (invoice == null) return Result<PaymentResponse>.Failure("Không tìm thấy hóa đơn.");

        if (invoice.Status == "Paid") return Result<PaymentResponse>.Failure("Hóa đơn này đã được thanh toán đủ.");
        if (invoice.Status == "Cancelled") return Result<PaymentResponse>.Failure("Không thể thanh toán cho hóa đơn đã hủy.");

        var paidAmount = invoice.Payments.Where(p => p.Status == "Confirmed").Sum(p => p.Amount);
        var finalAmount = invoice.FinalAmount ?? 0;
        var remainingAmount = finalAmount - paidAmount;

        if (request.Amount > remainingAmount)
            return Result<PaymentResponse>.Failure($"Số tiền thanh toán vượt quá số nợ còn lại ({remainingAmount:N0}).");

        if (request.PaymentMethod == "BANK_TRANSFER" && string.IsNullOrWhiteSpace(request.TransactionReference))
            return Result<PaymentResponse>.Failure("Thanh toán chuyển khoản bắt buộc phải có mã giao dịch (Mã tham chiếu).");

        var now = GetVietnamNow();

        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        try
        {
            var payment = new Payment
            {
                InvoiceId = request.InvoiceId,
                CenterId = centerId,
                Amount = request.Amount,
                PaymentMethod = request.PaymentMethod,
                TransactionReference = request.TransactionReference,
                ReceivedByUserId = userId,
                Status = "Confirmed",
                PaidAt = now,
                CreatedAt = now
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync(cancellationToken);

            var updateResult = await _invoiceService.UpdateStatusFromPaymentsAsync(invoice.InvoiceId, cancellationToken);
            if (!updateResult.IsSuccess)
            {
                return Result<PaymentResponse>.Failure(updateResult.Message);
            }

            // Automatically create a receipt for the payment
            var receiptNumber = $"PT-{now:yyMM}-{payment.PaymentId:D5}";
            var receipt = new Receipt
            {
                ReceiptNumber = receiptNumber,
                PaymentId = payment.PaymentId,
                CenterId = centerId,
                StudentName = invoice.Student?.FullName ?? "Unknown",
                ClassName = invoice.Class?.ClassName ?? "Unknown",
                CourseName = invoice.Class?.Course?.CourseName ?? "Unknown",
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod ?? "CASH",
                IssuedAt = now,
                IssuedByUserId = userId,
                Status = "Active"
            };
            _context.Receipts.Add(receipt);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            payment = await _context.Payments
                .Include(p => p.Invoice)
                .Include(p => p.ReceivedByUser)
                .FirstAsync(p => p.PaymentId == payment.PaymentId, cancellationToken);

            return Result<PaymentResponse>.Success(MapToResponse(payment), "Thanh toán thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment for invoice {InvoiceId}", request.InvoiceId);
            return Result<PaymentResponse>.Failure($"Lỗi hệ thống: {ex.Message} - {ex.InnerException?.Message}");
        }
    }

    public async Task<Result<PagedList<PaymentResponse>>> GetListAsync(int centerId, PaymentFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Payments
            .Include(p => p.Invoice)
            .Include(p => p.ReceivedByUser)
            .Where(p => p.CenterId == centerId)
            .AsNoTracking();

        if (filter.InvoiceId.HasValue) query = query.Where(p => p.InvoiceId == filter.InvoiceId.Value);
        if (!string.IsNullOrWhiteSpace(filter.PaymentMethod)) query = query.Where(p => p.PaymentMethod == filter.PaymentMethod);
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(p => p.Status == filter.Status);

        if (filter.DateFrom.HasValue)
        {
            var dateFrom = filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(p => p.CreatedAt >= dateFrom);
        }
        if (filter.DateTo.HasValue)
        {
            var dateTo = filter.DateTo.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(p => p.CreatedAt <= dateTo);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var responseItems = items.Select(MapToResponse).ToList();

        var pagedList = new PagedList<PaymentResponse>(
            responseItems,
            totalItems,
            filter.PageNumber,
            filter.PageSize,
            (int)Math.Ceiling(totalItems / (double)filter.PageSize)
        );

        return Result<PagedList<PaymentResponse>>.Success(pagedList);
    }

    public async Task<Result> CancelPaymentAsync(int paymentId, int centerId, int userId, string reason, CancellationToken cancellationToken = default)
    {
        var payment = await _context.Payments
            .Include(p => p.Invoice)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.CenterId == centerId, cancellationToken);

        if (payment == null) return Result.Failure("Không tìm thấy thanh toán.");
        if (payment.Status == "Cancelled") return Result.Failure("Thanh toán này đã bị hủy.");

        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            payment.Status = "Cancelled";
            payment.TransactionReference = string.IsNullOrWhiteSpace(payment.TransactionReference) 
                ? $"[Hủy: {reason}]" 
                : $"[Hủy: {reason}] {payment.TransactionReference}";
            
            await _context.SaveChangesAsync(cancellationToken);

            var updateResult = await _invoiceService.UpdateStatusFromPaymentsAsync(payment.InvoiceId, cancellationToken);
            if (!updateResult.IsSuccess)
            {
                return Result.Failure(updateResult.Message);
            }

            var receipt = await _context.Receipts.FirstOrDefaultAsync(r => r.PaymentId == paymentId && r.Status == "Active", cancellationToken);
            if (receipt != null)
            {
                receipt.Status = "Voided";
                receipt.VoidedAt = GetVietnamNow();
                receipt.VoidedByUserId = userId;
                receipt.VoidReason = reason;
                await _context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return Result.Success("Hủy thanh toán thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment {PaymentId}", paymentId);
            return Result.Failure("Có lỗi xảy ra khi hủy thanh toán.");
        }
    }

    private static PaymentResponse MapToResponse(Payment p)
    {
        return new PaymentResponse(
            p.PaymentId,
            p.InvoiceId,
            p.Invoice?.InvoiceCode ?? string.Empty,
            p.Amount,
            p.PaymentMethod ?? "CASH",
            p.TransactionReference,
            p.Status,
            p.CreatedAt,
            p.ReceivedByUser?.FullName ?? string.Empty
        );
    }

    private static DateTime GetVietnamNow()
    {
        try 
        { 
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")); 
        } 
        catch 
        { 
            return DateTime.UtcNow.AddHours(7); 
        }
    }
}
