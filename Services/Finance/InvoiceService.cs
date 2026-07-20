using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Contracts.Finance;
using Microsoft.Extensions.Logging;

namespace EduBridge.Services.Finance;

public sealed class InvoiceService : IInvoiceService
{
    private static readonly int[] AllowedPageSizes = [10, 20, 50, 100, 200, 500];
    private readonly AppDbContext _context;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(AppDbContext context, ILogger<InvoiceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<InvoiceResponse>> CreateInvoiceAsync(CreateInvoiceRequest request, int centerId, int userId, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == request.StudentId && s.CenterId == centerId && !s.IsDeleted, cancellationToken);
        if (student == null) return Result<InvoiceResponse>.Failure("Không tìm thấy học sinh hoặc học sinh không thuộc trung tâm.");

        var classEntity = await _context.Classes.Include(c => c.Course).AsNoTracking().FirstOrDefaultAsync(c => c.ClassId == request.ClassId && c.CenterId == centerId && !c.IsDeleted, cancellationToken);
        if (classEntity == null) return Result<InvoiceResponse>.Failure("Không tìm thấy lớp học hoặc lớp học không thuộc trung tâm.");

        var amount = request.Amount ?? classEntity.TuitionFee ?? classEntity.Course.TuitionFee ?? 0m;
        if (amount <= 0) return Result<InvoiceResponse>.Failure("Số tiền học phí phải lớn hơn 0.");

        var discountAmount = request.DiscountAmount ?? 0;
        if (discountAmount > amount) return Result<InvoiceResponse>.Failure("Số tiền chiết khấu không được lớn hơn học phí.");

        var invoiceCode = await GenerateInvoiceCodeAsync(centerId, cancellationToken);

        var invoice = new Invoice
        {
            CenterId = centerId,
            InvoiceCode = invoiceCode,
            StudentId = request.StudentId,
            ClassId = request.ClassId,
            Amount = amount,
            DiscountAmount = discountAmount,
            FinalAmount = amount - discountAmount,
            Description = request.Description,
            DiscountNote = request.DiscountNote,
            DueDate = request.DueDate ?? DateOnly.FromDateTime(GetVietnamNow().AddDays(7)),
            Status = "Unpaid",
            CreatedByUserId = userId,
            CreatedAt = GetVietnamNow()
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        invoice = await _context.Invoices
            .Include(i => i.Student)
            .Include(i => i.Class).ThenInclude(c => c.Course)
            .Include(i => i.CreatedByUser)
            .FirstAsync(i => i.InvoiceId == invoice.InvoiceId, cancellationToken);

        return Result<InvoiceResponse>.Success(MapToResponse(invoice), "Tạo hóa đơn thành công.");
    }

    public async Task<Result<InvoiceDetailResponse>> GetByIdAsync(int invoiceId, int centerId, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Student)
            .Include(i => i.Class).ThenInclude(c => c.Course)
            .Include(i => i.CreatedByUser)
            .Include(i => i.Payments.Where(p => p.Status != "Cancelled")).ThenInclude(p => p.ReceivedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.CenterId == centerId, cancellationToken);

        if (invoice == null) return Result<InvoiceDetailResponse>.Failure("Không tìm thấy hóa đơn.");

        var payments = invoice.Payments.OrderByDescending(p => p.CreatedAt).Select(p => new PaymentDto(
            p.PaymentId,
            p.Amount,
            p.PaymentMethod ?? string.Empty,
            p.TransactionReference,
            p.Status,
            p.CreatedAt,
            p.ReceivedByUser.FullName
        )).ToList();

        var paidAmount = invoice.Payments.Where(p => p.Status == "Confirmed").Sum(p => p.Amount);

        var detail = new InvoiceDetailResponse(
            invoice.InvoiceId,
            invoice.InvoiceCode,
            invoice.StudentId,
            invoice.Student.FullName,
            invoice.Student.StudentCode,
            invoice.ClassId,
            invoice.Class.ClassName,
            invoice.Class.CourseId,
            invoice.Class.Course.CourseName,
            invoice.Amount,
            invoice.DiscountAmount,
            invoice.FinalAmount ?? 0,
            invoice.DueDate,
            invoice.Status,
            paidAmount,
            (invoice.FinalAmount ?? 0) - paidAmount,
            invoice.CreatedAt,
            invoice.CreatedByUser.FullName,
            invoice.Description,
            invoice.DiscountNote,
            payments
        );

        return Result<InvoiceDetailResponse>.Success(detail);
    }

    public async Task<Result<PagedList<InvoiceResponse>>> GetListAsync(int centerId, InvoiceFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = AllowedPageSizes.Contains(filter.PageSize) ? filter.PageSize : 20;

        var query = _context.Invoices
            .Include(i => i.Student)
            .Include(i => i.Class).ThenInclude(c => c.Course)
            .Include(i => i.CreatedByUser)
            .Include(i => i.Payments.Where(p => p.Status == "Confirmed"))
                .ThenInclude(p => p.Receipts)
            .Where(i => i.CenterId == centerId)
            .AsNoTracking();

        if (filter.StudentId.HasValue) query = query.Where(i => i.StudentId == filter.StudentId.Value);
        if (filter.ClassId.HasValue) query = query.Where(i => i.ClassId == filter.ClassId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(i => i.Status == filter.Status);
        
        if (filter.DateFrom.HasValue) 
        {
            var dateFrom = filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(i => i.CreatedAt >= dateFrom);
        }
        if (filter.DateTo.HasValue) 
        {
            var dateTo = filter.DateTo.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(i => i.CreatedAt <= dateTo);
        }

        if (!string.IsNullOrWhiteSpace(filter.InvoiceCode))
        {
            var search = filter.InvoiceCode.Trim();
            if (int.TryParse(search, out int searchId))
            {
                query = query.Where(i => i.InvoiceCode.Contains(search) || i.InvoiceId == searchId);
            }
            else
            {
                query = query.Where(i => i.InvoiceCode.Contains(search));
            }
        }
        if (!string.IsNullOrWhiteSpace(filter.StudentName))
        {
            var search = filter.StudentName.Trim();
            query = query.Where(i => i.Student.FullName.Contains(search) || i.Student.StudentCode.Contains(search));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        query = filter.SortBy?.ToUpper() switch
        {
            "AMOUNT" => filter.SortDirection?.ToUpper() == "ASC" ? query.OrderBy(i => i.FinalAmount) : query.OrderByDescending(i => i.FinalAmount),
            "DUEDATE" => filter.SortDirection?.ToUpper() == "ASC" ? query.OrderBy(i => i.DueDate) : query.OrderByDescending(i => i.DueDate),
            _ => filter.SortDirection?.ToUpper() == "ASC" ? query.OrderBy(i => i.CreatedAt) : query.OrderByDescending(i => i.CreatedAt)
        };

        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        pageNumber = Math.Min(pageNumber, totalPages);

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var responseItems = items.Select(MapToResponse).ToList();

        var pagedList = new PagedList<InvoiceResponse>(
            responseItems,
            totalItems,
            pageNumber,
            pageSize,
            totalPages
        );

        return Result<PagedList<InvoiceResponse>>.Success(pagedList);
    }

    public async Task<Result> CancelAsync(int invoiceId, int centerId, int userId, string reason, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.CenterId == centerId, cancellationToken);
        if (invoice == null) return Result.Failure("Không tìm thấy hóa đơn.");

        if (invoice.Status != "Unpaid" && invoice.Status != "Unpaid")
        {
            return Result.Failure("Chỉ có thể hủy hóa đơn chưa thanh toán.");
        }

        invoice.Status = "Cancelled";
        invoice.UpdatedAt = GetVietnamNow();
        // Ghi lại reason vào bảng audit hoặc description (vì hiện tại chưa có CancelReason field)
        invoice.Description = string.IsNullOrWhiteSpace(invoice.Description) 
            ? $"[Đã hủy: {reason}]" 
            : $"{invoice.Description} | [Đã hủy: {reason}]";

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success("Hủy hóa đơn thành công.");
    }

    public async Task<Result> UpdateStatusFromPaymentsAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId, cancellationToken);

        if (invoice == null) return Result.Failure("Không tìm thấy hóa đơn.");
        if (invoice.Status == "Cancelled") return Result.Failure("Hóa đơn đã bị hủy.");

        var paidAmount = invoice.Payments.Where(p => p.Status == "Confirmed").Sum(p => p.Amount);
        var finalAmount = invoice.FinalAmount ?? 0;

        if (paidAmount >= finalAmount)
        {
            invoice.Status = "Paid";
        }
        else if (paidAmount > 0)
        {
            invoice.Status = "Partial";
        }
        else
        {
            var isOverdue = invoice.DueDate.HasValue && invoice.DueDate.Value < DateOnly.FromDateTime(GetVietnamNow());
            invoice.Status = isOverdue ? "Unpaid" : "Unpaid";
        }

        invoice.UpdatedAt = GetVietnamNow();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<PagedList<StudentDebtResponse>>> GetDebtListAsync(int centerId, DebtFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var validStatuses = new[] { "Unpaid", "Partial", "Unpaid" };
        var query = _context.Invoices
            .Include(i => i.Student)
            .Include(i => i.Payments)
            .Where(i => i.CenterId == centerId && validStatuses.Contains(i.Status) && !i.Student.IsDeleted)
            .AsNoTracking();

        if (filter.ClassId.HasValue)
        {
            query = query.Where(i => i.ClassId == filter.ClassId.Value);
        }

        var invoices = await query.ToListAsync(cancellationToken);

        var studentGroups = invoices.GroupBy(i => i.StudentId).Select(g =>
        {
            var student = g.First().Student;
            var totalFinal = g.Sum(i => i.FinalAmount ?? 0);
            var totalPaid = g.SelectMany(i => i.Payments).Where(p => p.Status == "Confirmed").Sum(p => p.Amount);
            var totalDebt = totalFinal - totalPaid;
            
            var oldestDueDate = g.Where(i => i.DueDate.HasValue).Min(i => i.DueDate);
            var overdueDays = 0;
            if (oldestDueDate.HasValue)
            {
                var today = DateOnly.FromDateTime(GetVietnamNow());
                if (oldestDueDate.Value < today)
                {
                    overdueDays = today.DayNumber - oldestDueDate.Value.DayNumber;
                }
            }

            return new StudentDebtResponse(
                student.StudentId,
                student.FullName,
                student.StudentCode,
                totalDebt,
                g.Count(),
                oldestDueDate,
                overdueDays
            );
        }).Where(s => s.TotalDebt > 0).ToList();

        var totalItems = studentGroups.Count;
        var items = studentGroups
            .OrderByDescending(s => s.OverdueDays)
            .ThenByDescending(s => s.TotalDebt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        var pagedList = new PagedList<StudentDebtResponse>(
            items,
            totalItems,
            filter.PageNumber,
            filter.PageSize,
            (int)Math.Ceiling(totalItems / (double)filter.PageSize)
        );

        return Result<PagedList<StudentDebtResponse>>.Success(pagedList);
    }

    public async Task<string> GenerateInvoiceCodeAsync(int centerId, CancellationToken cancellationToken = default)
    {
        var now = GetVietnamNow();
        var yearMonth = now.ToString("yyyyMM");

        var counter = await _context.InvoiceCodeCounters
            .FirstOrDefaultAsync(c => c.CenterId == centerId && c.YearMonth == yearMonth, cancellationToken);

        if (counter == null)
        {
            counter = new InvoiceCodeCounter { CenterId = centerId, YearMonth = yearMonth, LastNumber = 1 };
            _context.InvoiceCodeCounters.Add(counter);
        }
        else
        {
            counter.LastNumber += 1;
            _context.InvoiceCodeCounters.Update(counter);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Format: HD-202606-0001
        return $"HD-{yearMonth}-{counter.LastNumber:D4}";
    }

    private static InvoiceResponse MapToResponse(Invoice i)
    {
        var confirmedPayments = i.Payments?.Where(p => p.Status == "Confirmed").ToList() ?? new List<Payment>();
        var paidAmount = confirmedPayments.Sum(p => p.Amount);
        var latestReceiptId = confirmedPayments
            .SelectMany(p => p.Receipts)
            .Where(r => r.Status == "Active")
            .OrderByDescending(r => r.IssuedAt)
            .Select(r => (int?)r.ReceiptId)
            .FirstOrDefault();

        return new InvoiceResponse(
            i.InvoiceId,
            i.InvoiceCode,
            i.StudentId,
            i.Student?.FullName ?? string.Empty,
            i.Student?.StudentCode ?? string.Empty,
            i.ClassId,
            i.Class?.ClassName ?? string.Empty,
            i.Class?.CourseId ?? 0,
            i.Class?.Course?.CourseName ?? string.Empty,
            i.Amount,
            i.DiscountAmount,
            i.FinalAmount ?? 0,
            i.DueDate,
            i.Status,
            paidAmount,
            (i.FinalAmount ?? 0) - paidAmount,
            i.CreatedAt,
            i.CreatedByUser?.FullName ?? string.Empty,
            latestReceiptId
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
