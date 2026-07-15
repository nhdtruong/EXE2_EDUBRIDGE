using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Services.Auth;
using EduBridge.Services.Branches;
using EduBridge.Contracts.Branches;

namespace EduBridge.Pages;

[Authorize(Policy = "AdminOnly")]
public class AdminBranchesModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ICurrentCenterService _currentCenterService;
    private readonly IBranchManagementService _branchManagementService;

    public AdminBranchesModel(
        AppDbContext context, 
        ICurrentCenterService currentCenterService,
        IBranchManagementService branchManagementService)
    {
        _context = context;
        _currentCenterService = currentCenterService;
        _branchManagementService = branchManagementService;
    }

    public List<Branch> Branches { get; set; } = new();

    public string CurrentCenterName { get; private set; } = "Trung tâm";

    [BindProperty]
    public BranchCreateRequest CreateRequest { get; set; } = new();

    [BindProperty]
    public BranchUpdateRequest EditRequest { get; set; } = new();

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

        CurrentCenterName = await _context.Centers
            .AsNoTracking()
            .Where(c => c.CenterId == centerId.Value)
            .Select(c => c.CenterName)
            .FirstOrDefaultAsync(cancellationToken) ?? CurrentCenterName;

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

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        var centerId = await _currentCenterService.GetCenterIdAsync(cancellationToken);
        if (centerId == null)
        {
            return RedirectToPage("/Login");
        }

        CreateRequest.CenterId = centerId.Value;

        ModelState.Clear();
        if (!TryValidateModel(CreateRequest, nameof(CreateRequest)))
        {
            var errorMsgs = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(e => !string.IsNullOrEmpty(e)).ToList();
            var errorText = errorMsgs.Any() ? string.Join(", ", errorMsgs) : "Unknown validation error";
            TempData["ToastTitle"] = "Lỗi";
            TempData["ToastMessage"] = "Vui lòng kiểm tra lại thông tin nhập. (" + string.Join(", ", errorMsgs) + ")";
            TempData["ToastType"] = "error";
            TempData["OpenModal"] = "Create";
            return await OnGetAsync(cancellationToken);
        }

        var result = await _branchManagementService.CreateBranchAsync(CreateRequest, cancellationToken);

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
            {
                foreach (var msg in error.Value)
                {
                    ModelState.AddModelError($"CreateRequest.{error.Key}", msg);
                }
            }
            TempData["ToastTitle"] = "Lỗi";
            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = "error";
            TempData["OpenModal"] = "Create";
            return await OnGetAsync(cancellationToken);
        }

        TempData["ToastTitle"] = "Thành công";
        TempData["ToastMessage"] = "Thêm cơ sở thành công";
        TempData["ToastType"] = "success";

        return RedirectToPage("/AdminBranches");
    }

    public async Task<IActionResult> OnGetDetailAsync(int id, CancellationToken cancellationToken)
    {
        var centerId = await _currentCenterService.GetCenterIdAsync(cancellationToken);
        if (centerId == null) return new JsonResult(new { success = false, message = "Unauthorized" });

        var detail = await _branchManagementService.GetBranchDetailAsync(id, centerId.Value, cancellationToken);
        if (detail == null) return new JsonResult(new { success = false, message = "Not found" });

        return new JsonResult(new { success = true, data = detail });
    }

    public async Task<IActionResult> OnPostUpdateAsync(CancellationToken cancellationToken)
    {
        var centerId = await _currentCenterService.GetCenterIdAsync(cancellationToken);
        if (centerId == null) return RedirectToPage("/Login");

        EditRequest.CenterId = centerId.Value;

        ModelState.Clear();
        if (!TryValidateModel(EditRequest, nameof(EditRequest)))
        {
            TempData["ToastTitle"] = "Lỗi";
            TempData["ToastMessage"] = "Vui lòng kiểm tra lại thông tin nhập.";
            TempData["ToastType"] = "error";
            TempData["OpenModal"] = "Edit";
            return await OnGetAsync(cancellationToken);
        }

        var result = await _branchManagementService.UpdateBranchAsync(EditRequest.BranchId, EditRequest, cancellationToken);

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
            {
                foreach (var msg in error.Value)
                {
                    ModelState.AddModelError($"EditRequest.{error.Key}", msg);
                }
            }
            TempData["ToastTitle"] = "Lỗi";
            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = "error";
            TempData["OpenModal"] = "Edit";
            return await OnGetAsync(cancellationToken);
        }

        TempData["ToastTitle"] = "Thành công";
        TempData["ToastMessage"] = "Cập nhật cơ sở thành công";
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
