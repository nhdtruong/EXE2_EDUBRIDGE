using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Services.Auth;

namespace EduBridge.Pages;

[Authorize(Policy = "AdminOnly")]
public class AdminBranchesModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ICurrentCenterService _currentCenterService;

    public AdminBranchesModel(AppDbContext context, ICurrentCenterService currentCenterService)
    {
        _context = context;
        _currentCenterService = currentCenterService;
    }

    public List<Branch> Branches { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchKeyword { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchContact { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 20;

    public int[] PageSizeOptions { get; } = [10, 20, 50, 100, 200];
    public int TotalItems { get; private set; }
    public int TotalPages => TotalItems == 0 ? 1 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    public int FirstItemNumber => TotalItems == 0 ? 0 : (PageNumber - 1) * PageSize + 1;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["ActivePage"] = "AdminBranches";
        
        var centerId = await _currentCenterService.GetCenterIdAsync(cancellationToken);
        if (centerId == null)
        {
            return RedirectToPage("/Login");
        }

        var query = _context.Branches
            .Include(b => b.Center)
            .Include(b => b.HeadUser)
            .Where(b => b.CenterId == centerId)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchKeyword))
        {
            query = query.Where(b =>
                b.BranchName.Contains(SearchKeyword) ||
                b.BranchCode.Contains(SearchKeyword));
        }

        if (!string.IsNullOrWhiteSpace(SearchContact))
        {
            query = query.Where(b =>
                (b.Email != null && b.Email.Contains(SearchContact)) ||
                (b.PhoneNumber != null && b.PhoneNumber.Contains(SearchContact)));
        }

        if (!string.IsNullOrWhiteSpace(FilterStatus))
        {
            query = query.Where(b => b.Status == FilterStatus);
        }

        if (!PageSizeOptions.Contains(PageSize)) PageSize = 20;
        if (PageNumber < 1) PageNumber = 1;

        TotalItems = await query.CountAsync(cancellationToken);

        Branches = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(cancellationToken);
            
        return Page();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(int branchId, string currentStatus, CancellationToken cancellationToken)
    {
        var centerId = await _currentCenterService.GetCenterIdAsync(cancellationToken);
        if (centerId == null)
        {
            return RedirectToPage("/Login");
        }

        var branch = await _context.Branches
            .FirstOrDefaultAsync(b => b.BranchId == branchId && b.CenterId == centerId.Value, cancellationToken);

        if (branch == null)
        {
            return NotFound();
        }

        branch.Status = string.Equals(currentStatus, "Active", StringComparison.OrdinalIgnoreCase)
            ? "Inactive"
            : "Active";
        branch.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync(cancellationToken);

        TempData["ToastTitle"] = "Thành công";
        TempData["ToastMessage"] = "Cập nhật trạng thái cơ sở thành công";
        TempData["ToastType"] = "success";

        return RedirectToPage("/AdminBranches", new
        {
            SearchKeyword,
            SearchContact,
            FilterStatus,
            PageNumber,
            PageSize
        });
    }
}
