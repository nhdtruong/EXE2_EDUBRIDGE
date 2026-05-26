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
            if (User.Identity?.IsAuthenticated == true)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Vui lòng nhập đầy đủ thông tin đăng nhập.";
                return Page();
            }

            var loginIdentifier = Input.Email.Trim();
            var isEmailLogin = loginIdentifier.Contains('@');
            var normalizedEmail = isEmailLogin
                ? loginIdentifier.ToLowerInvariant()
                : null;
            var normalizedPhone = isEmailLogin
                ? null
                : NormalizePhoneNumber(loginIdentifier);
            var internationalPhone = normalizedPhone == null
                ? null
                : ToInternationalPhone(normalizedPhone);
            var expectedRoleCode = MapRoleGroupToRoleCode(Input.RoleGroup);

            if (expectedRoleCode == null)
            {
                ErrorMessage = "Vai trò đăng nhập không hợp lệ.";
                return Page();
            }

            var user = await _context.Users
                .AsTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => isEmailLogin
                    ? u.Email != null && u.Email == normalizedEmail
                    : u.NormalizedPhoneNumber == normalizedPhone ||
                      u.PhoneNumber != null &&
                      (
                          u.PhoneNumber
                              .Replace(" ", "")
                              .Replace("-", "")
                              .Replace(".", "")
                              .Replace("(", "")
                              .Replace(")", "")
                              .Replace("+", "") == normalizedPhone ||
                          u.PhoneNumber
                              .Replace(" ", "")
                              .Replace("-", "")
                              .Replace(".", "")
                              .Replace("(", "")
                              .Replace(")", "")
                              .Replace("+", "") == internationalPhone
                      ));

            if (user == null)
            {
                ErrorMessage = "Email/số điện thoại hoặc mật khẩu không đúng.";
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
                ErrorMessage = "Email/số điện thoại hoặc mật khẩu không đúng.";
                return Page();
            }

            user.LastLoginAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
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

        private static string NormalizePhoneNumber(string value)
        {
            var digits = new string(value.Where(char.IsDigit).ToArray());

            if (digits.StartsWith("84") && digits.Length >= 11)
            {
                return "0" + digits[2..];
            }

            return digits;
        }

        private static string ToInternationalPhone(string normalizedPhone)
        {
            return normalizedPhone.StartsWith('0') && normalizedPhone.Length >= 10
                ? "84" + normalizedPhone[1..]
                : normalizedPhone;
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
            [Required(ErrorMessage = "Vui lòng nhập email hoặc số điện thoại.")]
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
