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
    private readonly EduBridge.Services.Auth.IAccountAuthenticationService _authService;

    public SwitchRoleModel(EduBridge.Services.Auth.IAccountAuthenticationService authService)
    {
        _authService = authService;
    }

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

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
        {
            return RedirectToPage("/Index");
        }

        var availableRoles = await _authService.GetAvailableRolesAsync(userId);
        
        if (!availableRoles.Contains(roleCode.ToUpperInvariant()))
        {
            // The user doesn't have this role available
            TempData["ToastMessage"] = "Bạn không có quyền truy cập vai trò này.";
            TempData["ToastType"] = "error";
            return RedirectToPage("/Index");
        }

        // Generate new claims based on existing claims, but replace the Role claim and AvailableRoles claim
        var newClaims = new List<Claim>();
        foreach (var claim in User.Claims)
        {
            if (claim.Type == ClaimTypes.Role || claim.Type == "EduBridge:AvailableRoles")
            {
                continue; // Skip the old role claim and available roles claim
            }
            newClaims.Add(new Claim(claim.Type, claim.Value));
        }

        // Add the new role claim
        newClaims.Add(new Claim(ClaimTypes.Role, roleCode.ToUpperInvariant()));
        
        // Also update the AvailableRoles claim in the cookie so it stays fresh!
        newClaims.Add(new Claim("EduBridge:AvailableRoles", string.Join(",", availableRoles)));

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
