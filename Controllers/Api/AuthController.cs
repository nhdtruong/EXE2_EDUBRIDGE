using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EduBridge.Data;
using EduBridge.Services.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAccountAuthenticationService _authenticationService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        AppDbContext context,
        IAccountAuthenticationService authenticationService,
        IJwtTokenService jwtTokenService)
    {
        _context = context;
        _authenticationService = authenticationService;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiLoginResponse>> LoginAsync(
        ApiLoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authenticationService.AuthenticateAsync(
            request.LoginIdentifier,
            request.Password,
            expectedRoleCode: null,
            cancellationToken);

        if (!result.IsSuccess || result.User == null)
        {
            return Unauthorized(new ApiErrorResponse(result.ErrorMessage ?? "Đăng nhập thất bại."));
        }

        var accessToken = _jwtTokenService.CreateAccessToken(result.User);

        return Ok(new ApiLoginResponse(
            accessToken.Value,
            "Bearer",
            accessToken.ExpiresAtUtc,
            MapUser(result.User.UserId, result.User.FullName, result.User.Email, result.User.PhoneNumber,
                result.User.AvatarUrl, result.User.Role.RoleCode)));
    }

    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<ApiUserResponse>> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized(new ApiErrorResponse("Token không hợp lệ."));
        }

        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId && !u.IsDeleted && u.Status == "Active")
            .Select(u => new ApiUserResponse(
                u.UserId,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.AvatarUrl,
                u.Role.RoleCode))
            .FirstOrDefaultAsync(cancellationToken);

        return user == null
            ? Unauthorized(new ApiErrorResponse("Tài khoản không còn hoạt động."))
            : Ok(user);
    }

    [HttpGet("centers")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<IReadOnlyList<ApiCenterResponse>>> GetCentersAsync(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Unauthorized(new ApiErrorResponse("Token không hợp lệ."));
        }

        var memberships = await _context.CenterUsers
            .AsNoTracking()
            .Where(cu =>
                cu.UserId == userId &&
                cu.Status == "Active" &&
                cu.Center.Status == "Active")
            .Select(cu => new ApiCenterResponse(
                cu.CenterId,
                cu.Center.CenterName,
                cu.UserType))
            .ToListAsync(cancellationToken);

        var ownedCenters = await _context.Centers
            .AsNoTracking()
            .Where(c => c.OwnerUserId == userId && c.Status == "Active")
            .Select(c => new ApiCenterResponse(c.CenterId, c.CenterName, "OWNER"))
            .ToListAsync(cancellationToken);

        var centers = memberships
            .Concat(ownedCenters)
            .GroupBy(center => center.CenterId)
            .Select(group => group.First())
            .OrderBy(center => center.CenterName)
            .ToList();

        return Ok(centers);
    }

    private int? GetCurrentUserId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;
    }

    private static ApiUserResponse MapUser(
        int userId,
        string fullName,
        string? email,
        string? phoneNumber,
        string? avatarUrl,
        string roleCode)
    {
        return new ApiUserResponse(userId, fullName, email, phoneNumber, avatarUrl, roleCode);
    }
}

public sealed class ApiLoginRequest
{
    [Required]
    [MaxLength(150)]
    public string LoginIdentifier { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

public sealed record ApiLoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    ApiUserResponse User);

public sealed record ApiUserResponse(
    int UserId,
    string FullName,
    string? Email,
    string? PhoneNumber,
    string? AvatarUrl,
    string RoleCode);

public sealed record ApiCenterResponse(
    int CenterId,
    string CenterName,
    string UserType);

public sealed record ApiErrorResponse(string Message);
