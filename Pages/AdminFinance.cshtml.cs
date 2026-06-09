using System.Security.Claims;
using EduBridge.Contracts.Finance;
using EduBridge.Services.Finance;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using EduBridge.Data;

namespace EduBridge.Pages;

[Authorize(Roles = "OWNER")]
public class AdminFinanceModel : PageModel
{
    private readonly IFinanceSummaryService _summaryService;
    private readonly IInvoiceService _invoiceService;
    private readonly AppDbContext _context;

    public AdminFinanceModel(IFinanceSummaryService summaryService, IInvoiceService invoiceService, AppDbContext context)
    {
        _summaryService = summaryService;
        _invoiceService = invoiceService;
        _context = context;
    }

    public int CenterId { get; set; }

    public DashboardFinanceSummaryResponse Summary { get; set; } = null!;
    public List<ClassDebtSummaryResponse> ClassDebts { get; set; } = new();
    public PagedList<InvoiceResponse> Invoices { get; set; } = null!;

    [BindProperty(SupportsGet = true)]
    public string? InvoiceCode { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StudentName { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ClassId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? DateTo { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 20;

    public SelectList Classes { get; set; } = null!;

    public int[] PageSizeOptions { get; } = new[] { 10, 20, 50, 100, 200, 500 };

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var ownerUserId))
            return RedirectToPage("/Login");

        var center = await _context.Centers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.OwnerUserId == ownerUserId && c.Status == "Active", cancellationToken);

        if (center == null) return RedirectToPage("/Login");

        CenterId = center.CenterId;

        var now = DateTime.Now;
        var summaryResult = await _summaryService.GetDashboardSummaryAsync(CenterId, now.Month, now.Year, cancellationToken);
        if (summaryResult.IsSuccess && summaryResult.Value != null)
        {
            Summary = summaryResult.Value;
        }

        var classDebtsResult = await _summaryService.GetClassDebtSummariesAsync(CenterId, cancellationToken);
        if (classDebtsResult.IsSuccess && classDebtsResult.Value != null)
        {
            ClassDebts = classDebtsResult.Value;
        }

        var classes = await _context.Classes
            .Where(c => c.CenterId == CenterId && c.Status != "DELETED")
            .Select(c => new { c.ClassId, c.ClassName })
            .ToListAsync(cancellationToken);
        Classes = new SelectList(classes, "ClassId", "ClassName");

        var filter = new InvoiceFilterRequest(
            InvoiceCode: InvoiceCode,
            StudentName: StudentName,
            ClassId: ClassId,
            Status: Status,
            DateFrom: DateFrom,
            DateTo: DateTo,
            PageNumber: PageNumber,
            PageSize: PageSize
        );

        var invoiceResult = await _invoiceService.GetListAsync(CenterId, filter, cancellationToken);
        if (invoiceResult.IsSuccess && invoiceResult.Value != null)
        {
            Invoices = invoiceResult.Value;
        }

        return Page();
    }
}
