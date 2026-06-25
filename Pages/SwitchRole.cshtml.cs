using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages;

[Authorize]
public class SwitchRoleModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostAsync([FromForm] string roleCode)
    {
        if (string.IsNullOrWhiteSpace(roleCode))
        {
            return RedirectToPage("/Index");
        }

        var availableRolesClaim = User.FindFirst("EduBridge:AvailableRoles")?.Value;
        if (string.IsNullOrWhiteSpace(availableRolesClaim))
        {
            return RedirectToPage("/Index");
        }

        var availableRoles = availableRolesClaim.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (!availableRoles.Contains(roleCode.ToUpperInvariant()))
        {
            // The user doesn't have this role available
            TempData["ToastMessage"] = "Bạn không có quyền truy cập vai trò này.";
            TempData["ToastType"] = "error";
            return RedirectToPage("/Index");
        }

        // Generate new claims based on existing claims, but replace the Role claim
        var newClaims = new List<Claim>();
        foreach (var claim in User.Claims)
        {
            if (claim.Type == ClaimTypes.Role)
            {
                continue; // Skip the old role claim
            }
            newClaims.Add(new Claim(claim.Type, claim.Value));
        }

        // Add the new role claim
        newClaims.Add(new Claim(ClaimTypes.Role, roleCode.ToUpperInvariant()));

        var identity = new ClaimsIdentity(newClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            AllowRefresh = true
        };

        // Re-sign in the user with the new principal to update the cookie
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);

        TempData["ToastMessage"] = $"Đã chuyển sang giao diện {GetRoleName(roleCode)}.";

        return roleCode.ToUpperInvariant() switch
        {
            "OWNER" => RedirectToPage("/AdminDashboard"),
            "TEACHER" => RedirectToPage("/Teacher/Dashboard"),
            "PARENT" => RedirectToPage("/Messages"),
            _ => RedirectToPage("/Index")
        };
    }

    private static string GetRoleName(string roleCode)
    {
        return roleCode.ToUpperInvariant() switch
        {
            "OWNER" => "Chủ trung tâm / Quản lý",
            "TEACHER" => "Giáo viên",
            "PARENT" => "Phụ huynh",
            _ => "Người dùng"
        };
    }
}
