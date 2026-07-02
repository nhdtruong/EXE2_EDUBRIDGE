using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EduBridge.Data;
using EduBridge.Models;

namespace EduBridge.Pages.SystemAdmin;

[Authorize(Policy = "SystemAdminOnly")]
public class AuditLogsModel : PageModel
{
    private readonly AppDbContext _context;

    public AuditLogsModel(AppDbContext context)
    {
        _context = context;
    }

    public List<SystemAuditLog> Logs { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterDateRange { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 20;

    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public int FirstItemNumber => (PageNumber - 1) * PageSize + 1;
    public int[] PageSizeOptions = { 10, 20, 50, 100 };

    public async Task OnGetAsync()
    {
        ViewData["ActivePage"] = "SystemAuditLogs";
        
        var query = _context.SystemAuditLogs
            .Include(l => l.ActorUser)
            .Include(l => l.TargetCenter)
            .Include(l => l.TargetProject)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var searchLower = Search.ToLower();
            query = query.Where(l => l.Action.ToLower().Contains(searchLower) || l.EntityName.ToLower().Contains(searchLower) || (l.ActorUser != null && l.ActorUser.FullName.ToLower().Contains(searchLower)));
        }

        if (!string.IsNullOrWhiteSpace(FilterDateRange))
        {
            var parts = FilterDateRange.Split(" to ");
            if (parts.Length == 2 && DateTime.TryParse(parts[0], out var startDate) && DateTime.TryParse(parts[1], out var endDate))
            {
                var endOfDay = endDate.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.CreatedAt >= startDate.Date && l.CreatedAt <= endOfDay);
            }
            else if (DateTime.TryParse(FilterDateRange, out var exactDate))
            {
                var startOfDay = exactDate.Date;
                var endOfDay = exactDate.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.CreatedAt >= startOfDay && l.CreatedAt <= endOfDay);
            }
        }

        TotalItems = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);
        if (PageNumber < 1) PageNumber = 1;
        if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

        Logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .AsNoTracking()
            .ToListAsync();
    }
}
