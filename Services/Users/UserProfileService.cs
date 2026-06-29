using EduBridge.Contracts.Users;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Services.Classes;
using EduBridge.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EduBridge.Services.Users;

public sealed class UserProfileService : IUserProfileService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserProfileService> _logger;
    private readonly IFileStorageService _storageService;

    public UserProfileService(AppDbContext context, ILogger<UserProfileService> logger, IFileStorageService storageService)
    {
        _context = context;
        _logger = logger;
        _storageService = storageService;
    }

    public async Task<ClassOperationResult<UserProfileResponse>> GetProfileAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted, cancellationToken);

            if (user == null)
            {
                return ClassOperationResult<UserProfileResponse>.Failure("Không tìm thấy người dùng.");
            }

            var response = new UserProfileResponse
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Address = user.Address,
                IdentityNumber = user.IdentityNumber,
                IdentityIssuedDate = user.IdentityIssuedDate,
                IdentityIssuedPlace = user.IdentityIssuedPlace,
                CurrentAddress = user.CurrentAddress,
                PermanentAddress = user.PermanentAddress,
                Hometown = user.Hometown,
                PlaceOfBirth = user.PlaceOfBirth,
                Ethnicity = user.Ethnicity,
                Religion = user.Religion
            };

            return ClassOperationResult<UserProfileResponse>.Success(response, "Thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin hồ sơ cá nhân cho UserId: {UserId}", userId);
            return ClassOperationResult<UserProfileResponse>.Failure("Đã có lỗi xảy ra khi lấy thông tin.");
        }
    }

    public async Task<ClassOperationResult<bool>> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted, cancellationToken);

            if (user == null)
            {
                return ClassOperationResult<bool>.Failure("Không tìm thấy người dùng.");
            }

            // Kiểm tra trùng lặp email và số điện thoại
            var emailExists = await _context.Users
                .AnyAsync(u => u.UserId != userId && !u.IsDeleted && u.Email == request.Email, cancellationToken);
            if (emailExists)
            {
                return ClassOperationResult<bool>.Failure("Email đã được sử dụng bởi tài khoản khác.");
            }

            var phoneExists = await _context.Users
                .AnyAsync(u => u.UserId != userId && !u.IsDeleted && u.PhoneNumber == request.PhoneNumber, cancellationToken);
            if (phoneExists)
            {
                return ClassOperationResult<bool>.Failure("Số điện thoại đã được sử dụng bởi tài khoản khác.");
            }

            var cmndExists = await _context.Users
                .AnyAsync(u => u.UserId != userId && !u.IsDeleted && u.IdentityNumber == request.IdentityNumber, cancellationToken);
            if (cmndExists)
            {
                return ClassOperationResult<bool>.Failure("Số CMND/CCCD đã được sử dụng bởi tài khoản khác.");
            }

            user.FullName = request.FullName;
            user.Email = request.Email;
            user.PhoneNumber = request.PhoneNumber;
            user.DateOfBirth = request.DateOfBirth;
            user.Gender = request.Gender;
            user.IdentityNumber = request.IdentityNumber;
            user.IdentityIssuedDate = request.IdentityIssuedDate;
            user.IdentityIssuedPlace = request.IdentityIssuedPlace;
            user.CurrentAddress = request.CurrentAddress;
            user.PermanentAddress = request.PermanentAddress;
            user.Hometown = request.Hometown;
            user.PlaceOfBirth = request.PlaceOfBirth;
            user.Ethnicity = request.Ethnicity;
            user.Religion = request.Religion;

            // Optional: If you want to allow manually editing AvatarUrl, you could update it here.
            // However, usually if AvatarFile is uploaded, we update via UpdateAvatarAsync.
            // user.AvatarUrl = request.AvatarUrl; // Kept in sync if needed, but not necessary since the upload will overwrite it.

            // Update legacy address if needed
            user.Address = request.CurrentAddress ?? request.PermanentAddress;

            await _context.SaveChangesAsync(cancellationToken);

            return ClassOperationResult<bool>.Success(true, "Cập nhật thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật thông tin hồ sơ cá nhân cho UserId: {UserId}", userId);
            return ClassOperationResult<bool>.Failure("Đã có lỗi xảy ra khi cập nhật thông tin.");
        }
    }

    public async Task<ClassOperationResult<bool>> UpdateAvatarAsync(int userId, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted, cancellationToken);

            if (user == null)
            {
                return ClassOperationResult<bool>.Failure("Không tìm thấy người dùng.");
            }

            var oldAvatar = user.AvatarUrl;

            // Generate safe file name
            var prefix = string.IsNullOrEmpty(user.StaffCode) ? user.UserId.ToString() : user.StaffCode.ToLowerInvariant();
            var safeFileName = $"{prefix}-{Guid.NewGuid():N}{Path.GetExtension(fileName)}";

            // Save file
            user.AvatarUrl = await _storageService.SaveFileAsync(fileStream, safeFileName, "avatars", cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            // Delete old avatar
            if (!string.IsNullOrWhiteSpace(oldAvatar))
            {
                await _storageService.DeleteFileAsync(oldAvatar, cancellationToken);
            }

            return ClassOperationResult<bool>.Success(true, "Cập nhật ảnh đại diện thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật ảnh đại diện cho UserId: {UserId}", userId);
            return ClassOperationResult<bool>.Failure("Đã có lỗi xảy ra khi cập nhật ảnh đại diện.");
        }
    }
}
