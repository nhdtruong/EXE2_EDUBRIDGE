using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using EduBridge.DTOs.Centers;
using EduBridge.Services.SystemAdmin;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EduBridge.Pages.SystemAdmin;

[Authorize(Policy = "SystemAdminOnly")]
public class CreateCenterModel : PageModel
{
    private readonly ISystemAdminCenterService _centerService;

    public CreateCenterModel(ISystemAdminCenterService centerService)
    {
        _centerService = centerService;
    }

    [BindProperty]
    public CreateCenterRequestDto Input { get; set; } = new();

    public void OnGet()
    {
        ViewData["ActivePage"] = "SystemCenters";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ViewData["ActivePage"] = "SystemCenters";
            return Page();
        }

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var currentUserId))
        {
            ModelState.AddModelError(string.Empty, "Không tìm thấy thông tin xác thực.");
            ViewData["ActivePage"] = "SystemCenters";
            return Page();
        }

        var result = await _centerService.CreateCenterAsync(Input, currentUserId);
        
        if (result.Success)
        {
            TempData["ToastTitle"] = "Thành công";
            TempData["ToastMessage"] = "Đã thêm mới trung tâm thành công.";
            TempData["ToastType"] = "success";
            return RedirectToPage("/SystemAdmin/Centers");
        }
        else
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Có lỗi xảy ra");
            ViewData["ActivePage"] = "SystemCenters";
            return Page();
        }
    }
}
