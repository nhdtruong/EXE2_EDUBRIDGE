using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EduBridge.Services.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace EduBridge.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IAccountAuthenticationService _authenticationService;

        public LoginModel(IAccountAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [BindProperty]
        public LoginInput Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                // Optional: nếu đã login thì redirect theo role.
            }
        }

        [EnableRateLimiting("LoginRateLimit")]
        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            Response.Cookies.Delete("SupportCenterId");
            Response.Cookies.Delete("CurrentBranchId");

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Vui lòng nhập đầy đủ thông tin đăng nhập.";
                return Page();
            }

            var loginIdentifier = Input.Email.Trim();
            var authenticationResult = await _authenticationService.AuthenticateAsync(
                loginIdentifier,
                Input.Password,
                null,
                cancellationToken);

            if (!authenticationResult.IsSuccess || authenticationResult.User == null)
            {
                ErrorMessage = authenticationResult.ErrorMessage;
                return Page();
            }

            var user = authenticationResult.User;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, authenticationResult.PrimaryRole ?? "GUEST"),
                new Claim("EduBridge:AvailableRoles", string.Join(",", authenticationResult.AvailableRoles ?? new List<string>()))
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);

            TempData["ToastMessage"] = "Đăng nhập thành công.";

            return RedirectByRole(authenticationResult.PrimaryRole ?? "GUEST");
        }


        private IActionResult RedirectByRole(string roleCode)
        {
            return roleCode.ToUpperInvariant() switch
            {
                "SYSTEM_ADMIN" => RedirectToPage("/SystemAdmin/Centers"),
                "PROJECT_ADMIN" => RedirectToPage("/SystemAdmin/Centers"),
                "OWNER" => RedirectToPage("/AdminDashboard"),
                "BRANCH_MANAGER" => RedirectToPage("/AdminDashboard"),
                "TEACHER" => RedirectToPage("/Teacher/Dashboard"),
                "PARENT" => RedirectToPage("/Messages"),
                _ => RedirectToPage("/Index")
            };
        }

        public class LoginInput
        {
            [Required(ErrorMessage = "Vui lòng nhập email hoặc số điện thoại.")]
            [MaxLength(150)]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
            [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
            [MaxLength(100)]
            public string Password { get; set; } = string.Empty;
        }
    }
}
