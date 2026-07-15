using System.Security.Claims;
using EduBridge.Contracts.Staffs;
using EduBridge.Services.Staffs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Pages.AdminStaff;

[Authorize(Policy = "AdminOnly")]
public class EditModel : PageModel
{
    private readonly IStaffManagementService _service;
    private readonly EduBridge.Data.AppDbContext _context;

    public EditModel(IStaffManagementService service, EduBridge.Data.AppDbContext context)
    {
        _service = service;
        _context = context;
    }

    [BindProperty]
    public SaveStaffRequest Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int StaffId { get; set; }

    [BindProperty]
    public IFormFile? AvatarFile { get; set; }

    [BindProperty]
    public bool RemoveAvatar { get; set; }

    public string? ExistingAvatarUrl { get; set; }

    public List<EduBridge.Models.Branch> Branches { get; set; } = new();
    public bool IsSpecificBranch { get; set; }
    public int CurrentBranchId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var currentBranchIdStr = HttpContext.Request.Cookies["CurrentBranchId"];
        int.TryParse(currentBranchIdStr, out var currentBranchId);
        CurrentBranchId = currentBranchId;
        IsSpecificBranch = CurrentBranchId > 0;

        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return RedirectToPage("/Login");

        var centerId = await _context.CenterUsers.Where(cu => cu.UserId == ownerUserId.Value && cu.Status == "Active").Select(cu => cu.CenterId).FirstOrDefaultAsync(cancellationToken);
        if (centerId > 0)
        {
            Branches = await _context.Branches.Where(b => b.CenterId == centerId && b.Status != "DELETED").ToListAsync(cancellationToken);
        }

        var result = await _service.GetStaffAsync(ownerUserId.Value, id, cancellationToken);
        if (!result.IsSuccess) return NotFound();

        var t = result.Value!;
        StaffId = id;
        ExistingAvatarUrl = t.AvatarUrl;

        Input = new SaveStaffRequest
        {
            Roles = t.Roles,
            StaffCode = t.StaffCode,
            FullName = t.FullName,
            Specialization = t.Specialization,
            ExperienceYears = t.ExperienceYears,
            PhoneNumber = t.PhoneNumber ?? "",
            Email = t.Email,
            DateOfBirth = t.DateOfBirth,
            Gender = t.Gender,
            Ethnicity = t.Ethnicity,
            Religion = t.Religion,
            IdentityNumber = t.IdentityNumber,
            IdentityIssuedDate = t.IdentityIssuedDate,
            IdentityIssuedPlace = t.IdentityIssuedPlace,
            CurrentAddress = t.CurrentAddress,
            PermanentAddress = t.PermanentAddress,
            Hometown = t.Hometown,
            PlaceOfBirth = t.PlaceOfBirth,
            IsActive = t.Status == "Active",
            BranchIds = t.BranchIds ?? new List<int>()
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            // Re-fetch avatar url so the UI doesn't break
            var oId = GetCurrentUserId();
            if (oId != null)
            {
                var tr = await _service.GetStaffAsync(oId.Value, StaffId, cancellationToken);
                if (tr.IsSuccess) ExistingAvatarUrl = tr.Value!.AvatarUrl;
            }
            return Page();
        }

        var ownerUserId = GetCurrentUserId();
        if (ownerUserId == null) return RedirectToPage("/Login");

        var currentBranchIdStr = HttpContext.Request.Cookies["CurrentBranchId"];
        int.TryParse(currentBranchIdStr, out var currentBranchId);
        if (currentBranchId > 0)
        {
            Input.BranchIds = new List<int> { currentBranchId };
        }

        var result = await _service.UpdateAsync(ownerUserId.Value, StaffId, Input, cancellationToken);

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

            var tr = await _service.GetStaffAsync(ownerUserId.Value, StaffId, cancellationToken);
            if (tr.IsSuccess) ExistingAvatarUrl = tr.Value!.AvatarUrl;

            return Page();
        }

        if (RemoveAvatar)
        {
            await _service.RemoveAvatarAsync(ownerUserId.Value, StaffId, cancellationToken);
        }
        else if (AvatarFile != null)
        {
            await using var stream = AvatarFile.OpenReadStream();
            await _service.UpdateAvatarAsync(ownerUserId.Value, StaffId, stream, AvatarFile.FileName, AvatarFile.ContentType, cancellationToken);
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

