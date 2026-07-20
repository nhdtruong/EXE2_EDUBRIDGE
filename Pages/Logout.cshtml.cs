using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages
{
    public class LogoutModel : PageModel
    {
        public async Task<IActionResult> OnPostAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("SupportCenterId");
            Response.Cookies.Delete("CurrentBranchId");
            
            TempData["ToastMessage"] = "Đã đăng xuất.";
            return RedirectToPage("/Login", new { loggedOut = true });
        }
    }
}
