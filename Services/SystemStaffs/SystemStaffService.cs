using EduBridge.Contracts.SystemStaffs;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Services.Classes;
using EduBridge.Services.Storage;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace EduBridge.Services.SystemStaffs;

public class SystemStaffService : ISystemStaffService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SystemStaffService> _logger;
    private readonly IFileStorageService _storageService;

    public SystemStaffService(AppDbContext context, ILogger<SystemStaffService> logger, IFileStorageService storageService)
    {
        _context = context;
        _logger = logger;
        _storageService = storageService;
    }

    public async Task<ClassOperationResult<SystemStaffPagedResponse>> GetStaffsAsync(SystemStaffQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var targetRoles = new[] { "SYSTEM_ADMIN" };
            
            var queryable = _context.Users
                .Include(u => u.Role)
                .Where(u => !u.IsDeleted && targetRoles.Contains(u.Role.RoleCode))
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.NameCodeKeyword))
            {
                var kw = query.NameCodeKeyword.Trim();
                queryable = queryable.Where(u => u.FullName.Contains(kw) || 
                                               (u.StaffCode != null && u.StaffCode.Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(query.EmailPhoneKeyword))
            {
                var kw = query.EmailPhoneKeyword.Trim();
                queryable = queryable.Where(u => (u.Email != null && u.Email.Contains(kw)) || 
                                               (u.PhoneNumber != null && u.PhoneNumber.Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                queryable = queryable.Where(u => u.Status == query.Status);
            }



            var totalItems = await queryable.CountAsync(cancellationToken);
            var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)query.PageSize);
            
            var page = query.Page < 1 ? 1 : query.Page;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var items = await queryable
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(u => new SystemStaffListItemResponse(
                    u.UserId,
                    u.FullName,
                    u.Role.RoleCode,
                    u.StaffCode,
                    u.PhoneNumber,
                    u.Email,
                    u.AvatarUrl,
                    u.Status,
                    u.CreatedAt
                ))
                .ToListAsync(cancellationToken);

            var response = new SystemStaffPagedResponse(items, page, query.PageSize, totalItems, totalPages);
            return ClassOperationResult<SystemStaffPagedResponse>.Success(response, "Thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách nhân sự system admin.");
            return ClassOperationResult<SystemStaffPagedResponse>.Failure("Lỗi hệ thống khi tải danh sách.");
        }
    }

    public async Task<ClassOperationResult<SystemStaffDetailResponse>> GetStaffAsync(int staffUserId, CancellationToken cancellationToken = default)
    {
        var targetRoles = new[] { "SYSTEM_ADMIN" };
        var user = await _context.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == staffUserId && !u.IsDeleted && targetRoles.Contains(u.Role.RoleCode), cancellationToken);

        if (user == null)
        {
            return ClassOperationResult<SystemStaffDetailResponse>.Failure("Không tìm thấy nhân sự.");
        }

        var response = new SystemStaffDetailResponse(
            user.UserId,
            user.FullName,
            user.Role.RoleCode,
            user.StaffCode,
            user.PhoneNumber,
            user.Email,
            user.AvatarUrl,
            user.DateOfBirth,
            user.Gender,
            user.IdentityNumber,
            user.IdentityIssuedDate,
            user.IdentityIssuedPlace,
            user.Ethnicity,
            user.Religion,
            user.CurrentAddress,
            user.PermanentAddress,
            user.Hometown,
            user.PlaceOfBirth,
            user.Status,
            user.CreatedAt
        );

        return ClassOperationResult<SystemStaffDetailResponse>.Success(response, "Thành công");
    }

    public async Task<ClassOperationResult<SystemStaffMutationResponse>> CreateAsync(int currentUserId, SaveSystemStaffRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email && !u.IsDeleted, cancellationToken);
                if (emailExists)
                    return ClassOperationResult<SystemStaffMutationResponse>.Failure("Email này đã được sử dụng.");
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleCode == "SYSTEM_ADMIN", cancellationToken);
            if (role == null)
                return ClassOperationResult<SystemStaffMutationResponse>.Failure("Vai trò SYSTEM_ADMIN không tồn tại trong hệ thống.");

            var temporaryPassword = GenerateTemporaryPassword();
            var passwordHash = HashPassword(temporaryPassword);

            var staffCodeExists = await _context.Users.AnyAsync(u => u.StaffCode == request.StaffCode && !u.IsDeleted, cancellationToken);
            if (staffCodeExists)
                return ClassOperationResult<SystemStaffMutationResponse>.Failure("Mã nhân sự này đã tồn tại trong hệ thống.");

            var identityExists = await _context.Users.AnyAsync(u => u.IdentityNumber == request.IdentityNumber && !u.IsDeleted, cancellationToken);
            if (identityExists)
                return ClassOperationResult<SystemStaffMutationResponse>.Failure("Số CMND/CCCD này đã tồn tại trong hệ thống.");

            var phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber && !u.IsDeleted, cancellationToken);
            if (phoneExists)
                return ClassOperationResult<SystemStaffMutationResponse>.Failure("Số điện thoại này đã tồn tại trong hệ thống.");

            var newUser = new User
            {
                FullName = request.FullName,
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email,
                PhoneNumber = request.PhoneNumber,
                StaffCode = request.StaffCode,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                IdentityNumber = request.IdentityNumber,
                IdentityIssuedDate = request.IdentityIssuedDate,
                IdentityIssuedPlace = request.IdentityIssuedPlace,
                Ethnicity = request.Ethnicity,
                Religion = request.Religion,
                CurrentAddress = request.CurrentAddress,
                PermanentAddress = request.PermanentAddress,
                Hometown = request.Hometown,
                PlaceOfBirth = request.PlaceOfBirth,
                RoleId = role.RoleId,
                PasswordHash = passwordHash,
                Status = request.IsActive ? "ACTIVE" : "INACTIVE",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync(cancellationToken);

            // Log this action if possible

            var response = new SystemStaffMutationResponse(newUser.UserId, true, newUser.Status);
            return ClassOperationResult<SystemStaffMutationResponse>.Success(response, $"Tạo nhân sự thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo nhân sự system admin.");
            return ClassOperationResult<SystemStaffMutationResponse>.Failure("Lỗi hệ thống khi tạo nhân sự.");
        }
    }

    public async Task<ClassOperationResult<SystemStaffMutationResponse>> UpdateAsync(int currentUserId, int staffUserId, SaveSystemStaffRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var targetRoles = new[] { "SYSTEM_ADMIN" };
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == staffUserId && !u.IsDeleted && targetRoles.Contains(u.Role.RoleCode), cancellationToken);

            if (user == null)
                return ClassOperationResult<SystemStaffMutationResponse>.Failure("Không tìm thấy nhân sự.");

            if (!string.IsNullOrWhiteSpace(request.Email) && user.Email != request.Email)
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email && !u.IsDeleted && u.UserId != staffUserId, cancellationToken);
                if (emailExists)
                    return ClassOperationResult<SystemStaffMutationResponse>.Failure("Email này đã được sử dụng.");
            }
            
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleCode == "SYSTEM_ADMIN", cancellationToken);
            if (role == null)
                return ClassOperationResult<SystemStaffMutationResponse>.Failure("Vai trò SYSTEM_ADMIN không tồn tại trong hệ thống.");

            if (user.StaffCode != request.StaffCode)
            {
                var staffCodeExists = await _context.Users.AnyAsync(u => u.StaffCode == request.StaffCode && !u.IsDeleted && u.UserId != staffUserId, cancellationToken);
                if (staffCodeExists)
                    return ClassOperationResult<SystemStaffMutationResponse>.Failure("Mã nhân sự này đã tồn tại trong hệ thống.");
            }

            if (user.IdentityNumber != request.IdentityNumber)
            {
                var identityExists = await _context.Users.AnyAsync(u => u.IdentityNumber == request.IdentityNumber && !u.IsDeleted && u.UserId != staffUserId, cancellationToken);
                if (identityExists)
                    return ClassOperationResult<SystemStaffMutationResponse>.Failure("Số CMND/CCCD này đã tồn tại trong hệ thống.");
            }

            if (user.PhoneNumber != request.PhoneNumber)
            {
                var phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber && !u.IsDeleted && u.UserId != staffUserId, cancellationToken);
                if (phoneExists)
                    return ClassOperationResult<SystemStaffMutationResponse>.Failure("Số điện thoại này đã tồn tại trong hệ thống.");
            }

            user.FullName = request.FullName;
            user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email;
            user.PhoneNumber = request.PhoneNumber;
            user.StaffCode = request.StaffCode;
            user.DateOfBirth = request.DateOfBirth;
            user.Gender = request.Gender;
            user.IdentityNumber = request.IdentityNumber;
            user.IdentityIssuedDate = request.IdentityIssuedDate;
            user.IdentityIssuedPlace = request.IdentityIssuedPlace;
            user.Ethnicity = request.Ethnicity;
            user.Religion = request.Religion;
            user.CurrentAddress = request.CurrentAddress;
            user.PermanentAddress = request.PermanentAddress;
            user.Hometown = request.Hometown;
            user.PlaceOfBirth = request.PlaceOfBirth;
            user.RoleId = role.RoleId;
            user.Status = request.IsActive ? "ACTIVE" : "INACTIVE";

            await _context.SaveChangesAsync(cancellationToken);

            return ClassOperationResult<SystemStaffMutationResponse>.Success(new SystemStaffMutationResponse(user.UserId, false, user.Status), "Cập nhật nhân sự thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật nhân sự system admin.");
            return ClassOperationResult<SystemStaffMutationResponse>.Failure("Lỗi hệ thống khi cập nhật nhân sự.");
        }
    }

    public async Task<ClassOperationResult<SystemStaffMutationResponse>> SetStatusAsync(int currentUserId, int staffUserId, string status, CancellationToken cancellationToken = default)
    {
        var targetRoles = new[] { "SYSTEM_ADMIN" };
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == staffUserId && !u.IsDeleted && targetRoles.Contains(u.Role.RoleCode), cancellationToken);

        if (user == null)
            return ClassOperationResult<SystemStaffMutationResponse>.Failure("Không tìm thấy nhân sự.");
            
        if (user.UserId == currentUserId)
            return ClassOperationResult<SystemStaffMutationResponse>.Failure("Không thể tự khóa tài khoản của chính mình.");

        user.Status = status.ToUpper() == "ACTIVE" ? "ACTIVE" : "INACTIVE";
        await _context.SaveChangesAsync(cancellationToken);

        return ClassOperationResult<SystemStaffMutationResponse>.Success(new SystemStaffMutationResponse(user.UserId, false, user.Status), "Cập nhật trạng thái thành công");
    }

    public async Task<ClassOperationResult<ResetSystemStaffPasswordResponse>> ResetPasswordAsync(int currentUserId, int staffUserId, CancellationToken cancellationToken = default)
    {
        var targetRoles = new[] { "SYSTEM_ADMIN" };
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == staffUserId && !u.IsDeleted && targetRoles.Contains(u.Role.RoleCode), cancellationToken);

        if (user == null)
            return ClassOperationResult<ResetSystemStaffPasswordResponse>.Failure("Không tìm thấy nhân sự.");
            
        var temporaryPassword = GenerateTemporaryPassword();
        user.PasswordHash = HashPassword(temporaryPassword);
        await _context.SaveChangesAsync(cancellationToken);

        return ClassOperationResult<ResetSystemStaffPasswordResponse>.Success(new ResetSystemStaffPasswordResponse(user.UserId, temporaryPassword), "Đặt lại mật khẩu thành công");
    }

    public async Task<ClassOperationResult<bool>> DeleteStaffAsync(int currentUserId, int staffUserId, CancellationToken cancellationToken = default)
    {
        var targetRoles = new[] { "SYSTEM_ADMIN" };
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == staffUserId && !u.IsDeleted && targetRoles.Contains(u.Role.RoleCode), cancellationToken);

        if (user == null)
            return ClassOperationResult<bool>.Failure("Không tìm thấy nhân sự.");
            
        if (user.UserId == currentUserId)
            return ClassOperationResult<bool>.Failure("Không thể tự xóa tài khoản của chính mình.");

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedByUserId = currentUserId;

        await _context.SaveChangesAsync(cancellationToken);

        return ClassOperationResult<bool>.Success(true, "Đã xóa nhân sự thành công.");
    }

    public async Task<ClassOperationResult<SystemStaffMutationResponse>> UpdateAvatarAsync(int currentUserId, int staffUserId, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var targetRoles = new[] { "SYSTEM_ADMIN" };
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == staffUserId && !u.IsDeleted && targetRoles.Contains(u.Role.RoleCode), cancellationToken);

        if (user == null)
            return ClassOperationResult<SystemStaffMutationResponse>.Failure("Không tìm thấy nhân sự.");

        var oldAvatar = user.AvatarUrl;
        
        var prefix = string.IsNullOrEmpty(user.StaffCode) ? user.UserId.ToString() : user.StaffCode.ToLowerInvariant();
        var safeFileName = $"{prefix}-{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        user.AvatarUrl = await _storageService.SaveFileAsync(fileStream, safeFileName, "staffs", cancellationToken);
        
        await _context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(oldAvatar))
        {
            await _storageService.DeleteFileAsync(oldAvatar, cancellationToken);
        }

        return ClassOperationResult<SystemStaffMutationResponse>.Success(new SystemStaffMutationResponse(user.UserId, false, user.Status), "Cập nhật ảnh đại diện thành công.");
    }
    
    public async Task<ClassOperationResult<bool>> RemoveAvatarAsync(int currentUserId, int staffUserId, CancellationToken cancellationToken = default)
    {
        var targetRoles = new[] { "SYSTEM_ADMIN" };
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == staffUserId && !u.IsDeleted && targetRoles.Contains(u.Role.RoleCode), cancellationToken);

        if (user == null)
            return ClassOperationResult<bool>.Failure("Không tìm thấy nhân sự.");

        if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            await _storageService.DeleteFileAsync(user.AvatarUrl, cancellationToken);
            user.AvatarUrl = null;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return ClassOperationResult<bool>.Success(true, "Đã xóa ảnh đại diện.");
    }
    
    private string GenerateTemporaryPassword()
    {
        return "abc123456";
    }

    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
}
