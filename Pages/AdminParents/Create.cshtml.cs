using System.Security.Claims;
using EduBridge.Contracts.Parents;
using EduBridge.Services.Parents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages.AdminParents;

[Authorize(Policy = "AdminOnly")]
public sealed class CreateModel : PageModel
{
    private readonly IParentManagementService _service;
    public CreateModel(IParentManagementService service) => _service = service;
    [BindProperty] public SaveParentRequest Input { get; set; } = new();

    public void OnGet()
    {
        Input.Gender = "Nam";
        Input.Status = "Active";
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();
        var result = await _service.CreateAsync(GetUserId(), Input, cancellationToken);
        if (!result.IsSuccess)
        {
            AddErrors(result.Errors, result.Message);
            return Page();
        }
        SetToast(result.Message, true);
        return RedirectToPage("/AdminParents");
    }

    private int GetUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private void AddErrors(IReadOnlyDictionary<string, string[]> errors, string message)
    {
        foreach (var error in errors) foreach (var text in error.Value) ModelState.AddModelError(string.IsNullOrEmpty(error.Key) ? "" : $"Input.{error.Key}", text);
        ModelState.AddModelError("", message);
    }
    private void SetToast(string message, bool success)
    {
        TempData["ToastTitle"] = success ? "Thành công" : "Thất bại"; TempData["ToastType"] = success ? "success" : "error"; TempData["ToastMessage"] = message;
    }
}
