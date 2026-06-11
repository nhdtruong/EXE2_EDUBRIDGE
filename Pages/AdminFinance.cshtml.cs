using System.Security.Claims;
using EduBridge.Contracts.Finance;
using EduBridge.Services.Finance;
using EduBridge.Services.Settings;
using EduBridge.Services.Classes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;

namespace EduBridge.Pages;

[Authorize(Roles = "OWNER")]
public class AdminFinanceModel : PageModel
{
    private readonly IFinanceSummaryService _summaryService;
    private readonly IInvoiceService _invoiceService;
    private readonly ICenterSettingsService _centerSettingsService;
    private readonly IClassManagementService _classManagementService;

    public AdminFinanceModel(
        IFinanceSummaryService summaryService, 
        IInvoiceService invoiceService, 
        ICenterSettingsService centerSettingsService,
        IClassManagementService classManagementService)
    {
        _summaryService = summaryService;
        _invoiceService = invoiceService;
        _centerSettingsService = centerSettingsService;
        _classManagementService = classManagementService;
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

        var centerId = await _centerSettingsService.GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return RedirectToPage("/Login");

        CenterId = centerId.Value;

        var now = EduBridge.Helpers.TimeHelper.GetVietnamNow();
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

        var query = new EduBridge.Contracts.Classes.ClassQuery(null, null, null, null, 1, 1000);
        var classesResult = await _classManagementService.GetClassesAsync(ownerUserId, query, cancellationToken);
        if (classesResult.IsSuccess && classesResult.Value != null)
        {
            var classList = classesResult.Value.Items
                .OrderBy(c => c.ClassName)
                .Select(c => new { c.ClassId, c.ClassName })
                .ToList();
            Classes = new SelectList(classList, "ClassId", "ClassName");
        }
        else
        {
            Classes = new SelectList(new List<object>(), "ClassId", "ClassName");
        }

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
