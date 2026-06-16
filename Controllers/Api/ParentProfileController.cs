using System.Security.Claims;
using BCrypt.Net;
using EduBridge.Contracts.Classes;
using EduBridge.Data;
using EduBridge.Models.DTOs.ParentApp;
using EduBridge.Services.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/v1/parent/profile")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ParentOnly")]
public sealed class ParentProfileController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IFileStorageService _storageService;

    public ParentProfileController(AppDbContext context, IFileStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ParentProfileDto>>> GetProfile(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var profile = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId && !u.IsDeleted && u.Status == "Active" && u.Role.RoleCode == "PARENT")
            .Select(u => new ParentProfileDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                AvatarUrl = u.AvatarUrl,
                Address = u.Address
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile == null)
            return NotFound(new ApiResponse<ParentProfileDto>(false, "Không tìm thấy hồ sơ phụ huynh.", null));

        return Ok(new ApiResponse<ParentProfileDto>(true, "Thành công.", profile));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<ParentProfileDto>>> UpdateProfile(
        [FromBody] UpdateParentProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted && u.Status == "Active" && u.Role.RoleCode == "PARENT", cancellationToken);

        if (user == null)
            return NotFound(new ApiResponse<ParentProfileDto>(false, "Không tìm thấy hồ sơ phụ huynh.", null));

        var normalizedEmail = NormalizeNullable(request.Email);
        var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);

        if (normalizedEmail != null)
        {
            var emailExists = await _context.Users.AnyAsync(
                u => u.UserId != userId && !u.IsDeleted && u.Email != null && u.Email.ToLower() == normalizedEmail.ToLower(),
                cancellationToken);
            if (emailExists)
                return BadRequest(new ApiResponse<ParentProfileDto>(false, "Email đã được sử dụng.", null));
        }

        if (normalizedPhone != null)
        {
            var phoneExists = await _context.Users.AnyAsync(
                u => u.UserId != userId && !u.IsDeleted && u.NormalizedPhoneNumber == normalizedPhone,
                cancellationToken);
            if (phoneExists)
                return BadRequest(new ApiResponse<ParentProfileDto>(false, "Số điện thoại đã được sử dụng.", null));
        }

        user.FullName = request.FullName.Trim();
        user.Email = normalizedEmail;
        user.PhoneNumber = NormalizeNullable(request.PhoneNumber);
        user.NormalizedPhoneNumber = normalizedPhone;
        user.Address = NormalizeNullable(request.Address);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new ApiResponse<ParentProfileDto>(true, "Cập nhật hồ sơ thành công.", MapProfile(user)));
    }

    [HttpPut("password")]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(
        [FromBody] ChangeParentPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        if (request.NewPassword != request.ConfirmPassword)
            return BadRequest(new ApiResponse<bool>(false, "Xác nhận mật khẩu mới không khớp.", false));

        if (request.CurrentPassword == request.NewPassword)
            return BadRequest(new ApiResponse<bool>(false, "Mật khẩu mới phải khác mật khẩu hiện tại.", false));

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted && u.Status == "Active" && u.Role.RoleCode == "PARENT", cancellationToken);

        if (user == null)
            return NotFound(new ApiResponse<bool>(false, "Không tìm thấy tài khoản phụ huynh.", false));

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new ApiResponse<bool>(false, "Mật khẩu hiện tại không đúng.", false));

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new ApiResponse<bool>(true, "Đổi mật khẩu thành công.", true));
    }

    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<string>>> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new ApiResponse<string>(false, "Không có ảnh được chọn.", null));

        if (file.Length > 2 * 1024 * 1024)
            return BadRequest(new ApiResponse<string>(false, "Ảnh tối đa 2MB.", null));

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            return BadRequest(new ApiResponse<string>(false, "Chỉ hỗ trợ JPG, PNG, WEBP.", null));

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted && u.Status == "Active" && u.Role.RoleCode == "PARENT", cancellationToken);

        if (user == null)
            return NotFound(new ApiResponse<string>(false, "Không tìm thấy tài khoản phụ huynh.", null));

        var oldAvatar = user.AvatarUrl;
        var avatarUrl = await _storageService.SaveFileAsync(file, "parents", cancellationToken);
        user.AvatarUrl = avatarUrl;
        await _context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(oldAvatar) && !string.Equals(oldAvatar, avatarUrl, StringComparison.OrdinalIgnoreCase))
            await _storageService.DeleteFileAsync(oldAvatar, cancellationToken);

        return Ok(new ApiResponse<string>(true, "Cập nhật ảnh đại diện thành công.", avatarUrl));
    }

    [HttpDelete("avatar")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveAvatar(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted && u.Status == "Active" && u.Role.RoleCode == "PARENT", cancellationToken);

        if (user == null)
            return NotFound(new ApiResponse<bool>(false, "Không tìm thấy tài khoản phụ huynh.", false));

        var oldAvatar = user.AvatarUrl;
        user.AvatarUrl = null;
        await _context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(oldAvatar))
            await _storageService.DeleteFileAsync(oldAvatar, cancellationToken);

        return Ok(new ApiResponse<bool>(true, "Đã xóa ảnh đại diện.", true));
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private static ParentProfileDto MapProfile(Models.User user) => new()
    {
        UserId = user.UserId,
        FullName = user.FullName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        AvatarUrl = user.AvatarUrl,
        Address = user.Address
    };

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizePhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return null;
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }
}
