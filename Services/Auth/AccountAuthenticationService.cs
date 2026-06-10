using EduBridge.Data;
using EduBridge.Models;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Services.Auth;

public interface IAccountAuthenticationService
{
    Task<AccountAuthenticationResult> AuthenticateAsync(
        string loginIdentifier,
        string password,
        string? expectedRoleCode,
        CancellationToken cancellationToken = default);
}

public sealed class AccountAuthenticationService : IAccountAuthenticationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AccountAuthenticationService> _logger;

    public AccountAuthenticationService(
        AppDbContext context,
        ILogger<AccountAuthenticationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AccountAuthenticationResult> AuthenticateAsync(
        string loginIdentifier,
        string password,
        string? expectedRoleCode,
        CancellationToken cancellationToken = default)
    {
        var identifier = loginIdentifier.Trim();
        var isEmailLogin = identifier.Contains('@');
        var normalizedEmail = isEmailLogin ? identifier.ToLowerInvariant() : null;
        var normalizedPhone = isEmailLogin ? null : NormalizePhoneNumber(identifier);
        var internationalPhone = normalizedPhone == null
            ? null
            : ToInternationalPhone(normalizedPhone);

        var user = await _context.Users
            .AsTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u =>
                !u.IsDeleted &&
                (isEmailLogin
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
                      )),
                cancellationToken);

        if (user == null)
        {
            return AccountAuthenticationResult.Fail("Email/số điện thoại hoặc mật khẩu không đúng.");
        }

        if (user.Role == null)
        {
            _logger.LogWarning("User {UserId} has no role.", user.UserId);
            return AccountAuthenticationResult.Fail("Tài khoản chưa được phân quyền.");
        }

        if (!string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return AccountAuthenticationResult.Fail("Tài khoản đã bị khóa hoặc chưa được kích hoạt.");
        }

        if (!user.EmailConfirmed)
        {
            return AccountAuthenticationResult.Fail("Email của tài khoản chưa được xác nhận.");
        }

        if (!string.IsNullOrWhiteSpace(expectedRoleCode) &&
            !string.Equals(user.Role.RoleCode, expectedRoleCode, StringComparison.OrdinalIgnoreCase))
        {
            return AccountAuthenticationResult.Fail("Tài khoản không thuộc vai trò đăng nhập đã chọn.");
        }

        if (!await HasActiveMembershipAsync(user.UserId, user.Role.RoleCode, cancellationToken))
        {
            return AccountAuthenticationResult.Fail("Tài khoản không còn quyền hoạt động trong trung tâm.");
        }

        try
        {
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return AccountAuthenticationResult.Fail("Email/số điện thoại hoặc mật khẩu không đúng.");
            }
        }
        catch (BCrypt.Net.SaltParseException exception)
        {
            _logger.LogWarning(
                exception,
                "Invalid password hash format for user {UserId}.",
                user.UserId);

            return AccountAuthenticationResult.Fail(
                "Mật khẩu trong hệ thống chưa được cấu hình đúng. Vui lòng liên hệ quản trị viên.");
        }

        user.LastLoginAt = EduBridge.Helpers.TimeHelper.GetVietnamNow();
        await _context.SaveChangesAsync(cancellationToken);

        return AccountAuthenticationResult.Success(user);
    }

    private async Task<bool> HasActiveMembershipAsync(
        int userId,
        string roleCode,
        CancellationToken cancellationToken)
    {
        var normalizedRoleCode = roleCode.ToUpperInvariant();

        if (normalizedRoleCode == "OWNER")
        {
            return await _context.CenterUsers
                       .AsNoTracking()
                       .AnyAsync(cu =>
                           cu.UserId == userId &&
                           cu.UserType == "OWNER" &&
                           cu.Status == "Active" &&
                           cu.Center.Status == "Active",
                           cancellationToken) ||
                   await _context.Centers
                       .AsNoTracking()
                       .AnyAsync(c => c.OwnerUserId == userId && c.Status == "Active", cancellationToken);
        }

        if (normalizedRoleCode == "TEACHER")
        {
            return await _context.Teachers
                .AsNoTracking()
                .AnyAsync(t =>
                    t.UserId == userId &&
                    !t.IsDeleted &&
                    t.Status == "Active" &&
                    _context.CenterUsers.Any(cu =>
                        cu.CenterId == t.CenterId &&
                        cu.UserId == userId &&
                        cu.UserType == "TEACHER" &&
                        cu.Status == "Active"),
                    cancellationToken);
        }

        if (normalizedRoleCode == "PARENT")
        {
            return await _context.CenterUsers
                .AsNoTracking()
                .AnyAsync(cu =>
                    cu.UserId == userId &&
                    cu.UserType == "PARENT" &&
                    cu.Status == "Active" &&
                    cu.Center.Status == "Active",
                    cancellationToken);
        }

        return false;
    }

    private static string NormalizePhoneNumber(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());

        return digits.StartsWith("84") && digits.Length >= 11
            ? "0" + digits[2..]
            : digits;
    }

    private static string ToInternationalPhone(string normalizedPhone)
    {
        return normalizedPhone.StartsWith('0') && normalizedPhone.Length >= 10
            ? "84" + normalizedPhone[1..]
            : normalizedPhone;
    }
}

public sealed record AccountAuthenticationResult(
    bool IsSuccess,
    User? User,
    string? ErrorMessage)
{
    public static AccountAuthenticationResult Success(User user) => new(true, user, null);

    public static AccountAuthenticationResult Fail(string errorMessage) => new(false, null, errorMessage);
}
