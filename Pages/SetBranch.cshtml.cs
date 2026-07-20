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
            TempData["ToastMessage"] = "Đã chuyển cơ sở làm việc.";
            TempData["ToastType"] = "success";
        }
        else
        {
            Response.Cookies.Delete("CurrentBranchId");
            TempData["ToastTitle"] = "Thành công";
            TempData["ToastMessage"] = "Đã hiển thị tất cả cơ sở.";
            TempData["ToastType"] = "success";
        }

        var referer = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(referer))
        {
            return Redirect(referer);
        }
        return RedirectToPage("/AdminDashboard");
    }
}
