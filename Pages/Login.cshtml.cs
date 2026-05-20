using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EduBridge.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(AppDbContext context, ILogger<LoginModel> logger)
        {
            _context = context;
            _logger = logger;
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

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Vui lòng nhập đầy đủ thông tin đăng nhập.";
                return Page();
            }

            var email = Input.Email.Trim().ToLowerInvariant();
            var expectedRoleCode = MapRoleGroupToRoleCode(Input.RoleGroup);

            if (expectedRoleCode == null)
            {
                ErrorMessage = "Vai trò đăng nhập không hợp lệ.";
                return Page();
            }

            var user = await _context.Users
                .AsTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
            {
                ErrorMessage = "Email hoặc mật khẩu không đúng.";
                return Page();
            }

            if (user.Role == null)
            {
                _logger.LogWarning("User {UserId} has no role.", user.UserId);
                ErrorMessage = "Tài khoản chưa được phân quyền.";
                return Page();
            }

            if (!string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Tài khoản đã bị khóa hoặc chưa được kích hoạt.";
                return Page();
            }

            if (!user.EmailConfirmed)
            {
                ErrorMessage = "Email của tài khoản chưa được xác nhận.";
                return Page();
            }

            if (!string.Equals(user.Role.RoleCode, expectedRoleCode, StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Tài khoản không thuộc vai trò đăng nhập đã chọn.";
                return Page();
            }

            bool passwordValid;

            try
            {
                passwordValid = BCrypt.Net.BCrypt.Verify(Input.Password, user.PasswordHash);
            }
            catch (BCrypt.Net.SaltParseException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid password hash format for user {UserId}.",
                    user.UserId);

                ErrorMessage = "Mật khẩu trong hệ thống chưa được cấu hình đúng. Vui lòng liên hệ quản trị viên.";
                return Page();
            }

            if (!passwordValid)
            {
                ErrorMessage = "Email hoặc mật khẩu không đúng.";
                return Page();
            }

            user.LastLoginAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.RoleCode)
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

            return RedirectByRole(user.Role.RoleCode);
        }

        private static string? MapRoleGroupToRoleCode(string roleGroup)
        {
            return roleGroup?.Trim().ToUpperInvariant() switch
            {
                "ADMIN" => "OWNER",
                "OWNER" => "OWNER",
                "TEACHER" => "TEACHER",
                "PARENT" => "PARENT",
                _ => null
            };
        }

        private IActionResult RedirectByRole(string roleCode)
        {
            return roleCode.ToUpperInvariant() switch
            {
                "OWNER" => RedirectToPage("/AdminDashboard"),
                "TEACHER" => RedirectToPage("/Teacher/Dashboard"),
                "PARENT" => RedirectToPage("/Messages"),
                _ => RedirectToPage("/Index")
            };
        }

        public class LoginInput
        {
            [Required(ErrorMessage = "Vui lòng nhập email.")]
            [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
            [MaxLength(150)]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
            [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
            [MaxLength(100)]
            public string Password { get; set; } = string.Empty;

            [Required]
            [MaxLength(20)]
            public string RoleGroup { get; set; } = string.Empty;
        }
    }
}
