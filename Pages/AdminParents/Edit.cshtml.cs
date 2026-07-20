using System.Security.Claims;
using EduBridge.Contracts.Parents;
using EduBridge.Services.Parents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages.AdminParents;

[Authorize(Policy = "AdminOnly")]
public sealed class EditModel : PageModel
{
    private readonly IParentManagementService _service;
    public EditModel(IParentManagementService service) => _service = service;
    [BindProperty] public SaveParentRequest Input { get; set; } = new();
    [BindProperty] public int ParentUserId { get; set; }
    public IReadOnlyList<ParentChildResponse> Children { get; private set; } = [];
    public IReadOnlyList<LinkableStudentResponse> LinkableStudents { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _service.GetParentAsync(GetUserId(), id, cancellationToken);
        if (!result.IsSuccess || result.Value == null) return NotFound();
        ParentUserId = id;
        Children = result.Value.Children;
        LinkableStudents = (await _service.GetLinkableStudentsAsync(
            GetUserId(),
            id,
            cancellationToken: cancellationToken)).Value ?? [];
        Input = new SaveParentRequest
        {
            FullName = result.Value.FullName, PhoneNumber = result.Value.PhoneNumber ?? "", Email = result.Value.Email,
            DateOfBirth = result.Value.DateOfBirth, Gender = result.Value.Gender, IdentityNumber = result.Value.IdentityNumber,
            IdentityIssuedDate = result.Value.IdentityIssuedDate, IdentityIssuedPlace = result.Value.IdentityIssuedPlace,
            Ethnicity = result.Value.Ethnicity, Religion = result.Value.Religion,
            CurrentAddress = result.Value.CurrentAddress, PermanentAddress = result.Value.PermanentAddress,
            Hometown = result.Value.Hometown, PlaceOfBirth = result.Value.PlaceOfBirth,
            Status = result.Value.Status
        };
        return Page();
    }

    public async Task<IActionResult> OnPostLinkStudentAsync(int studentId, CancellationToken cancellationToken)
    {
        if (studentId == 0)
        {
            TempData["ToastTitle"] = "Thất bại";
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Vui lòng chọn học sinh để liên kết.";
            return RedirectToPage("/AdminParents/Edit", new { id = ParentUserId });
        }

        var result = await _service.LinkStudentAsync(GetUserId(), ParentUserId, studentId, cancellationToken);
        TempData["ToastTitle"] = result.IsSuccess ? "Thành công" : "Thất bại";
        TempData["ToastType"] = result.IsSuccess ? "success" : "error";
        TempData["ToastMessage"] = result.Message;
        return RedirectToPage("/AdminParents/Edit", new { id = ParentUserId });
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { await LoadChildren(); return Page(); }
        var result = await _service.UpdateAsync(GetUserId(), ParentUserId, Input, cancellationToken);
        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors) foreach (var text in error.Value) ModelState.AddModelError(string.IsNullOrEmpty(error.Key) ? "" : $"Input.{error.Key}", text);
            ModelState.AddModelError("", result.Message);
            await LoadChildren();
            return Page();
        }
        TempData["ToastTitle"] = "Thành công"; TempData["ToastType"] = "success"; TempData["ToastMessage"] = result.Message;
        return RedirectToPage("/AdminParents");
    }

    private async Task LoadChildren()
    {
        var result = await _service.GetParentAsync(GetUserId(), ParentUserId);
        Children = result.Value?.Children ?? [];
        LinkableStudents = (await _service.GetLinkableStudentsAsync(GetUserId(), ParentUserId)).Value ?? [];
    }
    private int GetUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
