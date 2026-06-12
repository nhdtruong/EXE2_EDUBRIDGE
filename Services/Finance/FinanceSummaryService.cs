using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EduBridge.Data;
using EduBridge.Contracts.Finance;
using Microsoft.Extensions.Logging;

namespace EduBridge.Services.Finance;

public sealed class FinanceSummaryService : IFinanceSummaryService
{
    private readonly AppDbContext _context;
    private readonly ILogger<FinanceSummaryService> _logger;

    public FinanceSummaryService(AppDbContext context, ILogger<FinanceSummaryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<DashboardFinanceSummaryResponse>> GetDashboardSummaryAsync(int centerId, int month, int year, CancellationToken cancellationToken = default)
    {
        var totalRevenue = await _context.Payments
            .Where(p => p.CenterId == centerId && p.Status == "Confirmed" && p.PaidAt.Month == month && p.PaidAt.Year == year)
            .SumAsync(p => p.Amount, cancellationToken);

        var activeInvoices = await _context.Invoices
            .Where(i => i.CenterId == centerId && (i.Status == "Unpaid" || i.Status == "Partial" || i.DueDate < DateOnly.FromDateTime(EduBridge.Helpers.TimeHelper.GetVietnamNow())))
            .Select(i => new {
                FinalAmount = i.FinalAmount ?? 0,
                PaidAmount = i.Payments.Where(p => p.Status == "Confirmed").Sum(p => p.Amount)
            })
            .ToListAsync(cancellationToken);

        var totalDebt = activeInvoices.Sum(i => i.FinalAmount - i.PaidAmount);

        var totalInvoicesCreated = await _context.Invoices
            .Where(i => i.CenterId == centerId && i.CreatedAt.Month == month && i.CreatedAt.Year == year)
            .CountAsync(cancellationToken);

        var totalInvoicesPaid = await _context.Invoices
            .Where(i => i.CenterId == centerId && i.Status == "Paid")
            .Where(i => i.Payments.Any(p => p.Status == "Confirmed" && p.PaidAt.Month == month && p.PaidAt.Year == year))
            .CountAsync(cancellationToken);

        var result = new DashboardFinanceSummaryResponse(
            totalRevenue,
            totalDebt,
            totalInvoicesCreated,
            totalInvoicesPaid
        );

        return Result<DashboardFinanceSummaryResponse>.Success(result);
    }

    public async Task<Result<List<ClassDebtSummaryResponse>>> GetClassDebtSummariesAsync(int centerId, CancellationToken cancellationToken = default)
    {
        var classes = await _context.Classes
            .Where(c => c.CenterId == centerId && c.Status == "Active" && !c.IsDeleted)
            .Select(c => new
            {
                c.ClassId,
                c.ClassName,
                Invoices = _context.Invoices.Where(i => i.ClassId == c.ClassId).Select(i => new
                {
                    FinalAmount = i.FinalAmount ?? 0,
                    PaidAmount = i.Payments.Where(p => p.Status == "Confirmed").Sum(p => p.Amount),
                    Status = i.Status
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var result = new List<ClassDebtSummaryResponse>();

        foreach (var c in classes)
        {
            var totalExpected = c.Invoices.Sum(i => i.FinalAmount);
            var totalCollected = c.Invoices.Sum(i => i.PaidAmount);
            var totalDebt = totalExpected - totalCollected;
            var unpaidStudentsCount = c.Invoices.Count(i => i.Status == "Unpaid" || i.Status == "Partial");

            result.Add(new ClassDebtSummaryResponse(
                c.ClassId,
                c.ClassName,
                totalExpected,
                totalCollected,
                totalDebt > 0 ? totalDebt : 0,
                unpaidStudentsCount
            ));
        }

        return Result<List<ClassDebtSummaryResponse>>.Success(result.OrderByDescending(r => r.TotalDebt).ToList());
    }
}
