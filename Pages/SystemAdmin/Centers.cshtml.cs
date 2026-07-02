using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EduBridge.Data;
using EduBridge.Models;

namespace EduBridge.Pages.SystemAdmin;

[Authorize(Policy = "SystemAdminOnly")]
public class CentersModel : PageModel
{
    private readonly AppDbContext _context;

    public CentersModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Center> Centers { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchName { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchContact { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterDateRange { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 20;

    public int[] PageSizeOptions { get; } = [10, 20, 50, 100, 200, 500];
    public int TotalItems { get; private set; }
    public int TotalPages => TotalItems == 0 ? 1 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    public int FirstItemNumber => TotalItems == 0 ? 0 : (PageNumber - 1) * PageSize + 1;


    public async Task OnGetAsync()
    {
        ViewData["ActivePage"] = "SystemCenters";
        
        var query = _context.Centers
            .Include(c => c.Project)
            .Include(c => c.OwnerUser)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchName))
        {
            query = query.Where(c => c.CenterName.Contains(SearchName));
        }

        if (!string.IsNullOrWhiteSpace(SearchContact))
        {
            query = query.Where(c => (c.Email != null && c.Email.Contains(SearchContact)) || 
                                     (c.PhoneNumber != null && c.PhoneNumber.Contains(SearchContact)));
        }

        if (!string.IsNullOrWhiteSpace(FilterDateRange))
        {
            var parts = FilterDateRange.Split(" to ");
            if (parts.Length == 2 && DateTime.TryParse(parts[0], out var from) && DateTime.TryParse(parts[1], out var to))
            {
                query = query.Where(c => c.CreatedAt.Date >= from.Date && c.CreatedAt.Date <= to.Date);
            }
        }

        if (!string.IsNullOrWhiteSpace(FilterStatus))
        {
            query = query.Where(c => c.Status == FilterStatus);
        }

        if (!PageSizeOptions.Contains(PageSize)) PageSize = 20;
        if (PageNumber < 1) PageNumber = 1;

        TotalItems = await query.CountAsync();

        Centers = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    public IActionResult OnPostEnterSupportMode(int centerId)
    {
        var cookieOptions = new CookieOptions
        {
            Expires = DateTime.UtcNow.AddHours(2),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict
        };
        Response.Cookies.Append("SupportCenterId", centerId.ToString(), cookieOptions);
        TempData["ToastTitle"] = "Thành công";
        TempData["ToastMessage"] = "Đã vào chế độ hỗ trợ cho trung tâm " + centerId;
        TempData["ToastType"] = "success";

        // Ghi Audit Log (Giả lập đơn giản, lý tưởng là dùng IAuditLogger)
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdStr, out var userId))
        {
            var log = new SystemAuditLog
            {
                ActorUserId = userId,
                TargetCenterId = centerId,
                Action = "ENTER_SUPPORT_MODE",
                EntityName = "Center",
                EntityId = centerId.ToString()
            };
            _context.SystemAuditLogs.Add(log);
            _context.SaveChanges();
        }

        return RedirectToPage("/AdminDashboard");
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(int centerId, string? searchName, string? searchContact, string? filterDateRange, string? filterStatus, int pageNumber, int pageSize)
    {
        var center = await _context.Centers.FindAsync(centerId);
        if (center != null)
        {
            center.Status = center.Status == "Active" ? "Inactive" : "Active";
            await _context.SaveChangesAsync();
            TempData["ToastTitle"] = "Thành công";
            TempData["ToastMessage"] = "Đã cập nhật trạng thái trung tâm!";
            TempData["ToastType"] = "success";
        }

        return RedirectToPage(new
        {
            SearchName = searchName,
            SearchContact = searchContact,
            FilterDateRange = filterDateRange,
            FilterStatus = filterStatus,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }
}
