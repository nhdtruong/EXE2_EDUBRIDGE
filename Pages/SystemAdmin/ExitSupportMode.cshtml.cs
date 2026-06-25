using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace EduBridge.Pages.SystemAdmin;

[Authorize(Policy = "SystemAdminOnly")]
public class ExitSupportModeModel : PageModel
{
    public IActionResult OnPost()
    {
        Response.Cookies.Delete("SupportCenterId");
        TempData["ToastTitle"] = "Thành công";
        TempData["ToastMessage"] = "Đã thoát chế độ hỗ trợ.";
        TempData["ToastType"] = "success";
        return RedirectToPage("/SystemAdmin/Centers");
    }
}
