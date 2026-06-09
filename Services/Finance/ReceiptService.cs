using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Contracts.Finance;
using Microsoft.Extensions.Logging;
using EduBridge.Utils;

namespace EduBridge.Services.Finance;

public sealed class ReceiptService : IReceiptService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReceiptService> _logger;

    public ReceiptService(AppDbContext context, ILogger<ReceiptService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<ReceiptResponse>> IssueReceiptAsync(IssueReceiptRequest request, int centerId, int userId, CancellationToken cancellationToken = default)
    {
        var payment = await _context.Payments
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Student)
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Class)
                    .ThenInclude(c => c.Course)
            .FirstOrDefaultAsync(p => p.PaymentId == request.PaymentId && p.CenterId == centerId, cancellationToken);

        if (payment == null) return Result<ReceiptResponse>.Failure("Không tìm thấy thanh toán.");
        if (payment.Status != "Confirmed") return Result<ReceiptResponse>.Failure("Chỉ có thể xuất phiếu thu cho thanh toán đã xác nhận.");

        var existingReceipt = await _context.Receipts
            .FirstOrDefaultAsync(r => r.PaymentId == request.PaymentId && r.Status == "Active", cancellationToken);
        
        if (existingReceipt != null)
        {
            return Result<ReceiptResponse>.Failure("Thanh toán này đã được xuất phiếu thu.");
        }

        var now = GetVietnamNow();
        var receiptNumber = $"PT-{now:yyMM}-{payment.PaymentId:D5}";

        var receipt = new Receipt
        {
            ReceiptNumber = receiptNumber,
            PaymentId = payment.PaymentId,
            CenterId = centerId,
            StudentName = payment.Invoice.Student.FullName,
            ClassName = payment.Invoice.Class.ClassName,
            CourseName = payment.Invoice.Class.Course.CourseName,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod ?? "CASH",
            IssuedAt = now,
            IssuedByUserId = userId,
            Status = "Active"
        };

        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync(cancellationToken);

        receipt = await _context.Receipts
            .Include(r => r.IssuedByUser)
            .FirstAsync(r => r.ReceiptId == receipt.ReceiptId, cancellationToken);

        return Result<ReceiptResponse>.Success(MapToResponse(receipt), "Xuất phiếu thu thành công.");
    }

    public async Task<Result<ReceiptResponse>> GetByIdAsync(int receiptId, int centerId, CancellationToken cancellationToken = default)
    {
        var receipt = await _context.Receipts
            .Include(r => r.IssuedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ReceiptId == receiptId && r.CenterId == centerId, cancellationToken);

        if (receipt == null) return Result<ReceiptResponse>.Failure("Không tìm thấy phiếu thu.");

        return Result<ReceiptResponse>.Success(MapToResponse(receipt));
    }

    public async Task<Result<PagedList<ReceiptResponse>>> GetListAsync(int centerId, ReceiptFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Receipts
            .Include(r => r.IssuedByUser)
            .Where(r => r.CenterId == centerId)
            .AsNoTracking();

        if (filter.PaymentId.HasValue) query = query.Where(r => r.PaymentId == filter.PaymentId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(r => r.Status == filter.Status);
        
        if (filter.DateFrom.HasValue)
        {
            var dateFrom = filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(r => r.IssuedAt >= dateFrom);
        }
        if (filter.DateTo.HasValue)
        {
            var dateTo = filter.DateTo.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(r => r.IssuedAt <= dateTo);
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var search = filter.Keyword.Trim();
            query = query.Where(r => r.ReceiptNumber.Contains(search) || r.StudentName.Contains(search));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.IssuedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var responseItems = items.Select(MapToResponse).ToList();

        var pagedList = new PagedList<ReceiptResponse>(
            responseItems,
            totalItems,
            filter.PageNumber,
            filter.PageSize,
            (int)Math.Ceiling(totalItems / (double)filter.PageSize)
        );

        return Result<PagedList<ReceiptResponse>>.Success(pagedList);
    }

    public async Task<Result> VoidReceiptAsync(int receiptId, int centerId, int userId, string reason, CancellationToken cancellationToken = default)
    {
        var receipt = await _context.Receipts
            .FirstOrDefaultAsync(r => r.ReceiptId == receiptId && r.CenterId == centerId, cancellationToken);

        if (receipt == null) return Result.Failure("Không tìm thấy phiếu thu.");
        if (receipt.Status == "Voided") return Result.Failure("Phiếu thu này đã bị hủy trước đó.");

        if (string.IsNullOrWhiteSpace(reason)) return Result.Failure("Vui lòng nhập lý do hủy phiếu thu.");

        receipt.Status = "Voided";
        receipt.VoidedAt = GetVietnamNow();
        receipt.VoidedByUserId = userId;
        receipt.VoidReason = reason;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success("Hủy phiếu thu thành công.");
    }

    public async Task<Result<ReceiptPrintResponse>> GetForPrintAsync(int receiptId, int centerId, CancellationToken cancellationToken = default)
    {
        var receipt = await _context.Receipts
            .Include(r => r.Center)
            .Include(r => r.IssuedByUser)
            .Include(r => r.Payment)
                .ThenInclude(p => p.Invoice)
                    .ThenInclude(i => i.Student)
                        .ThenInclude(s => s.ParentUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ReceiptId == receiptId && r.CenterId == centerId, cancellationToken);

        if (receipt == null) return Result<ReceiptPrintResponse>.Failure("Không tìm thấy phiếu thu.");
        if (receipt.Status == "Voided") return Result<ReceiptPrintResponse>.Failure("Phiếu thu này đã bị hủy, không thể in.");

        var invoice = receipt.Payment.Invoice;
        var student = invoice.Student;
        var center = receipt.Center;
        
        // Calculate total paid before this receipt for this invoice
        var totalPaidBeforeThis = await _context.Payments
            .Where(p => p.InvoiceId == invoice.InvoiceId && p.PaymentId != receipt.PaymentId && p.Status == "Confirmed" && p.PaidAt <= receipt.Payment.PaidAt)
            .SumAsync(p => p.Amount, cancellationToken);

        var response = new ReceiptPrintResponse(
            ReceiptId: receipt.ReceiptId,
            ReceiptNumber: receipt.ReceiptNumber,
            IssuedAt: receipt.IssuedAt,
            PaymentMethod: receipt.PaymentMethod,
            CenterName: center.CenterName,
            CenterAddress: center.Address ?? string.Empty,
            CenterPhoneNumber: center.PhoneNumber,
            CenterEmail: center.Email,
            InvoiceCode: invoice.InvoiceCode,
            StudentName: receipt.StudentName,
            StudentCode: student.StudentCode,
            ClassName: receipt.ClassName,
            CourseName: receipt.CourseName,
            ParentName: student.ParentUser?.FullName,
            ParentPhoneNumber: student.ParentUser?.PhoneNumber,
            Description: invoice.Description,
            InvoiceAmount: invoice.Amount,
            DiscountAmount: invoice.DiscountAmount,
            AmountPaid: receipt.Amount,
            TotalPaidBeforeThis: totalPaidBeforeThis,
            FinalInvoiceAmount: invoice.FinalAmount ?? (invoice.Amount - invoice.DiscountAmount),
            AmountInWords: NumberToWordsHelper.ConvertToWords(receipt.Amount),
            IssuedByUserName: receipt.IssuedByUser?.FullName ?? string.Empty,
            TransactionReference: receipt.Payment.TransactionReference,
            Status: receipt.Status
        );

        return Result<ReceiptPrintResponse>.Success(response);
    }

    private static ReceiptResponse MapToResponse(Receipt r)
    {
        return new ReceiptResponse(
            r.ReceiptId,
            r.ReceiptNumber,
            r.PaymentId,
            r.StudentName,
            r.ClassName,
            r.CourseName,
            r.Amount,
            r.PaymentMethod,
            r.IssuedAt,
            r.IssuedByUser?.FullName ?? string.Empty,
            r.Status,
            r.VoidReason
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
