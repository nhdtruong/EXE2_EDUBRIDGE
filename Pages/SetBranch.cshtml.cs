using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace EduBridge.Pages;

[Authorize]
[IgnoreAntiforgeryToken]
public class SetBranchModel : PageModel
{
    public IActionResult OnPost(int branchId)
    {
        if (branchId > 0)
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(30),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            };
            Response.Cookies.Append("CurrentBranchId", branchId.ToString(), cookieOptions);
            TempData["ToastTitle"] = "Thành công";
            TempData["ToastMessage"] = "Đổi cơ sở thành công";
            TempData["ToastType"] = "success";
        }
        else
        {
            Response.Cookies.Delete("CurrentBranchId");
            TempData["ToastTitle"] = "Thành công";
            TempData["ToastMessage"] = "Đổi cơ sở thành công";
            TempData["ToastType"] = "success";
        }

        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role == "TEACHER")
        {
            return RedirectToPage("/Teacher/Dashboard");
        }
        else if (role == "OWNER" || role == "SYSTEM_ADMIN")
        {
            return RedirectToPage("/AdminDashboard");
        }
        
        return RedirectToPage("/Index");
    }
}
