using System.Security.Claims;
using EduBridge.Contracts.Staffs;
using EduBridge.Services.Staffs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Pages.AdminStaff;

[Authorize(Policy = "AdminOnly")]
public class CreateModel : PageModel
{
    private readonly IStaffManagementService _service;
    private readonly EduBridge.Data.AppDbContext _context;

    public CreateModel(IStaffManagementService service, EduBridge.Data.AppDbContext context)
    {
        _service = service;
        _context = context;
    }

    [BindProperty]
    public SaveStaffRequest Input { get; set; } = new();

    [BindProperty]
    public IFormFile? AvatarFile { get; set; }

    public List<EduBridge.Models.Branch> Branches { get; set; } = new();
    public bool IsSpecificBranch { get; set; }
    public int CurrentBranchId { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var currentBranchIdStr = HttpContext.Request.Cookies["CurrentBranchId"];
        int.TryParse(currentBranchIdStr, out var currentBranchId);
        CurrentBranchId = currentBranchId;
        IsSpecificBranch = CurrentBranchId > 0;

        var ownerUserId = GetCurrentUserId();
        if (ownerUserId != null)
        {
            var centerId = await _context.CenterUsers.Where(cu => cu.UserId == ownerUserId.Value && cu.Status == "Active").Select(cu => cu.CenterId).FirstOrDefaultAsync(cancellationToken);
            if (centerId > 0)
            {
                Branches = await _context.Branches.Where(b => b.CenterId == centerId && b.Status != "DELETED").ToListAsync(cancellationToken);
            }
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return RedirectToPage("/Login");

        var currentBranchIdStr = HttpContext.Request.Cookies["CurrentBranchId"];
        int.TryParse(currentBranchIdStr, out var currentBranchId);
        if (currentBranchId > 0)
        {
            Input.BranchIds = new List<int> { currentBranchId };
        }

        var result = await _service.CreateAsync(ownerUserId.Value, Input, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Errors != null)
            {
                foreach (var (key, errors) in result.Errors)
                foreach (var error in errors)
                    ModelState.AddModelError(string.IsNullOrEmpty(key) ? string.Empty : $"Input.{key}", error);
            }
            else ModelState.AddModelError(string.Empty, result.Message);
            
            TempData["ToastType"] = "error";
            TempData["ToastTitle"] = "Thất bại";
            TempData["ToastMessage"] = result.Message;
            
            return Page();
        }

        if (AvatarFile != null)
        {
            var staffUserId = result.Value!.UserId;
            await using var stream = AvatarFile.OpenReadStream();
            await _service.UpdateAvatarAsync(ownerUserId.Value, staffUserId, stream, AvatarFile.FileName, AvatarFile.ContentType, cancellationToken);
        }

        TempData["ToastType"] = "success";
        TempData["ToastTitle"] = "Thành công";
        TempData["ToastMessage"] = result.Message;

        return RedirectToPage("/AdminStaff");
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}

