using System.Data;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using EduBridge.Contracts.Teachers;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Services.Classes;
using EduBridge.Services.Storage;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Services.Teachers;

public sealed class TeacherManagementService : ITeacherManagementService
{
    private static readonly int[] AllowedPageSizes = [10, 20, 50, 100, 200, 500];
    private readonly AppDbContext _context;
    private readonly ILogger<TeacherManagementService> _logger;
    private readonly IFileStorageService _storageService;

    public TeacherManagementService(AppDbContext context, ILogger<TeacherManagementService> logger, IFileStorageService storageService)
    {
        _context = context;
        _logger = logger;
        _storageService = storageService;
    }

    public async Task<ClassOperationResult<TeacherPagedResponse>> GetTeachersAsync(
        int ownerUserId, TeacherQuery query, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<TeacherPagedResponse>("Không tìm thấy trung tâm đang hoạt động.");

        query.Page = Math.Max(1, query.Page);
        query.PageSize = AllowedPageSizes.Contains(query.PageSize) ? query.PageSize : 20;
        query.Keyword = NormalizeOptional(query.Keyword);
        query.Status = NormalizeOptional(query.Status);

        if (query.Status is not null and not ("Active" or "Inactive"))
            return Fail<TeacherPagedResponse>("Trạng thái lọc không hợp lệ.", "Status");

        var teachers = _context.Teachers.AsNoTracking()
            .Include(t => t.User)
            .Where(t => t.CenterId == centerId && !t.IsDeleted);

        if (query.Keyword != null)
        {
            var phone = NormalizePhone(query.Keyword);
            teachers = teachers.Where(t => 
                t.TeacherCode.Contains(query.Keyword) || 
                t.User.FullName.Contains(query.Keyword) || 
                (t.User.PhoneNumber != null && t.User.PhoneNumber.Contains(query.Keyword)) ||
                (t.User.NormalizedPhoneNumber != null && t.User.NormalizedPhoneNumber.Contains(phone)));
        }

        if (query.Status != null) teachers = teachers.Where(t => t.Status == query.Status);

        var total = await teachers.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)query.PageSize));
        query.Page = Math.Min(query.Page, totalPages);

        var items = await teachers
            .OrderByDescending(t => t.TeacherId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new TeacherListItemResponse(
                t.UserId, t.TeacherCode, t.User.FullName, t.User.PhoneNumber, t.User.Email, t.User.AvatarUrl,
                t.Classes.Count(c => !c.IsDeleted && c.Status != "CANCELLED"),
                t.Classes.Where(c => !c.IsDeleted && c.Status != "CANCELLED").SelectMany(c => c.Enrollments).Count(e => e.Status == "Đang học"),
                t.Status, t.User.CreatedAt))
            .ToListAsync(cancellationToken);

        return ClassOperationResult<TeacherPagedResponse>.Success(
            new TeacherPagedResponse(items, query.Page, query.PageSize, total, totalPages), "Tải danh sách giáo viên thành công.");
    }

    public async Task<ClassOperationResult<TeacherDetailResponse>> GetTeacherAsync(
        int ownerUserId, int teacherUserId, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<TeacherDetailResponse>("Không tìm thấy trung tâm đang hoạt động.");

        var teacher = await _context.Teachers.AsNoTracking()
            .Include(t => t.User)
            .Where(t => t.CenterId == centerId && t.UserId == teacherUserId && !t.IsDeleted)
            .Select(t => new TeacherDetailResponse(
                t.UserId, t.TeacherCode, t.User.FullName, t.User.PhoneNumber, t.User.Email, t.User.AvatarUrl,
                t.User.DateOfBirth, t.User.Gender ?? string.Empty, t.User.Ethnicity, t.User.Religion, t.User.IdentityNumber ?? string.Empty,
                t.User.IdentityIssuedDate, t.User.IdentityIssuedPlace, t.User.CurrentAddress, t.User.PermanentAddress,
                t.User.Hometown, t.User.PlaceOfBirth, t.Status, t.User.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return teacher == null
            ? Fail<TeacherDetailResponse>("Không tìm thấy giáo viên trong trung tâm.")
            : ClassOperationResult<TeacherDetailResponse>.Success(teacher, "Tải thông tin giáo viên thành công.");
    }

    public async Task<ClassOperationResult<TeacherMutationResponse>> CreateAsync(
        int ownerUserId, SaveTeacherRequest request, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<TeacherMutationResponse>("Không tìm thấy trung tâm đang hoạt động.");

        var validation = ValidateAndNormalize(request);
        if (validation != null) return validation;

        var teacherCodeExists = await _context.Teachers.AsNoTracking().AnyAsync(
            t => t.CenterId == centerId && !t.IsDeleted && t.TeacherCode == request.TeacherCode, cancellationToken);
        if (teacherCodeExists) return Fail<TeacherMutationResponse>("Mã giáo viên đã tồn tại.", "TeacherCode");

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            
            var phone = NormalizePhone(request.PhoneNumber);
            var existingByPhone = await _context.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => !u.IsDeleted && u.NormalizedPhoneNumber == phone, cancellationToken);
            var existingByEmail = request.Email == null ? null : await _context.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => !u.IsDeleted && u.Email == request.Email, cancellationToken);

            if (existingByPhone != null && existingByEmail != null && existingByPhone.UserId != existingByEmail.UserId)
                return Fail<TeacherMutationResponse>("Số điện thoại và email đang thuộc hai tài khoản khác nhau.");

            var existing = existingByPhone ?? existingByEmail;
            var created = false;

            if (existing != null && existing.Role.RoleCode != "TEACHER")
                return Fail<TeacherMutationResponse>("Email hoặc số điện thoại đang thuộc tài khoản không phải giáo viên.");

            if (existing == null)
            {
                var roleId = await _context.Roles.Where(r => r.RoleCode == "TEACHER").Select(r => (int?)r.RoleId).FirstOrDefaultAsync(cancellationToken);
                if (roleId == null) return Fail<TeacherMutationResponse>("Database chưa có role TEACHER.");

                existing = new User
                {
                    RoleId = roleId.Value, FullName = request.FullName, PhoneNumber = request.PhoneNumber,
                    NormalizedPhoneNumber = phone, Email = request.Email, DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender, IdentityNumber = request.IdentityNumber, IdentityIssuedDate = request.IdentityIssuedDate,
                    IdentityIssuedPlace = request.IdentityIssuedPlace, Ethnicity = request.Ethnicity, Religion = request.Religion,
                    CurrentAddress = request.CurrentAddress, PermanentAddress = request.PermanentAddress,
                    Hometown = request.Hometown, PlaceOfBirth = request.PlaceOfBirth,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("edubridge2026"), EmailConfirmed = true,
                    Status = "Active", IsDeleted = false, CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(existing);
                await _context.SaveChangesAsync(cancellationToken);
                created = true;
            }

            var membership = await _context.CenterUsers.FirstOrDefaultAsync(cu =>
                cu.CenterId == centerId && cu.UserId == existing.UserId && cu.UserType == "TEACHER", cancellationToken);
            
            if (membership == null)
            {
                _context.CenterUsers.Add(new CenterUser
                {
                    CenterId = centerId.Value, UserId = existing.UserId, UserType = "TEACHER",
                    Status = request.IsActive ? "Active" : "Inactive", CreatedAt = DateTime.UtcNow
                });
            }

            var existingTeacher = await _context.Teachers.FirstOrDefaultAsync(t =>
                t.CenterId == centerId && t.UserId == existing.UserId && !t.IsDeleted, cancellationToken);

            if (existingTeacher != null)
                return Fail<TeacherMutationResponse>("Giáo viên này đã có hồ sơ trong trung tâm.");

            var newTeacher = new Teacher
            {
                CenterId = centerId.Value, UserId = existing.UserId, TeacherCode = request.TeacherCode,
                Status = request.IsActive ? "Active" : "Inactive", IsDeleted = false
            };
            _context.Teachers.Add(newTeacher);

            if (request.IsActive) existing.Status = "Active";

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            return ClassOperationResult<TeacherMutationResponse>.Success(
                new TeacherMutationResponse(existing.UserId, created, request.IsActive ? "Active" : "Inactive"),
                created ? "Đã tạo hồ sơ giáo viên mới." : "Đã thêm hồ sơ giáo viên vào trung tâm.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Could not create teacher.");
            return Fail<TeacherMutationResponse>("Không thể lưu thông tin do dữ liệu trùng hoặc vừa thay đổi.");
        }
    }

    public async Task<ClassOperationResult<TeacherMutationResponse>> UpdateAsync(
        int ownerUserId, int teacherUserId, SaveTeacherRequest request, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<TeacherMutationResponse>("Không tìm thấy trung tâm đang hoạt động.");

        var validation = ValidateAndNormalize(request);
        if (validation != null) return validation;

        var teacher = await _context.Teachers.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.CenterId == centerId && t.UserId == teacherUserId && !t.IsDeleted, cancellationToken);
        
        if (teacher == null) return Fail<TeacherMutationResponse>("Không tìm thấy hồ sơ giáo viên.");

        var teacherCodeExists = await _context.Teachers.AsNoTracking().AnyAsync(
            t => t.CenterId == centerId && !t.IsDeleted && t.TeacherCode == request.TeacherCode && t.TeacherId != teacher.TeacherId, cancellationToken);
        if (teacherCodeExists) return Fail<TeacherMutationResponse>("Mã giáo viên đã tồn tại.", "TeacherCode");

        var phone = NormalizePhone(request.PhoneNumber);
        var duplicateUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => !u.IsDeleted && u.UserId != teacherUserId &&
            (u.NormalizedPhoneNumber == phone || (request.Email != null && u.Email == request.Email) ||
             (request.IdentityNumber != null && u.IdentityNumber == request.IdentityNumber)), cancellationToken);

        if (duplicateUser != null)
        {
            if (duplicateUser.NormalizedPhoneNumber == phone) return Fail<TeacherMutationResponse>("Số điện thoại đã tồn tại.", "PhoneNumber");
            if (request.Email != null && duplicateUser.Email == request.Email) return Fail<TeacherMutationResponse>("Email đã tồn tại.", "Email");
            if (request.IdentityNumber != null && duplicateUser.IdentityNumber == request.IdentityNumber) return Fail<TeacherMutationResponse>("CCCD/CMND đã tồn tại.", "IdentityNumber");
        }

        teacher.TeacherCode = request.TeacherCode;
        teacher.Status = request.IsActive ? "Active" : "Inactive";

        teacher.User.FullName = request.FullName;
        teacher.User.PhoneNumber = request.PhoneNumber;
        teacher.User.NormalizedPhoneNumber = phone;
        teacher.User.Email = request.Email;
        teacher.User.DateOfBirth = request.DateOfBirth;
        teacher.User.Gender = request.Gender;
        teacher.User.IdentityNumber = request.IdentityNumber;
        teacher.User.IdentityIssuedDate = request.IdentityIssuedDate;
        teacher.User.IdentityIssuedPlace = request.IdentityIssuedPlace;
        teacher.User.Ethnicity = request.Ethnicity;
        teacher.User.Religion = request.Religion;
        teacher.User.CurrentAddress = request.CurrentAddress;
        teacher.User.PermanentAddress = request.PermanentAddress;
        teacher.User.Hometown = request.Hometown;
        teacher.User.PlaceOfBirth = request.PlaceOfBirth;

        var membership = await _context.CenterUsers.FirstOrDefaultAsync(cu => cu.CenterId == centerId && cu.UserId == teacherUserId && cu.UserType == "TEACHER", cancellationToken);
        if (membership != null) membership.Status = request.IsActive ? "Active" : "Inactive";

        if (request.IsActive) teacher.User.Status = "Active";
        else
        {
            var otherActive = await _context.CenterUsers.AsNoTracking().AnyAsync(cu => cu.UserId == teacherUserId && cu.Status == "Active" && cu.CenterId != centerId, cancellationToken);
            teacher.User.Status = otherActive ? "Active" : "Inactive";
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ClassOperationResult<TeacherMutationResponse>.Success(new(teacherUserId, false, request.IsActive ? "Active" : "Inactive"), "Cập nhật hồ sơ giáo viên thành công.");
    }

    public async Task<ClassOperationResult<TeacherMutationResponse>> SetStatusAsync(
        int ownerUserId, int teacherUserId, string status, CancellationToken cancellationToken = default)
    {
        if (status is not ("Active" or "Inactive")) return Fail<TeacherMutationResponse>("Trạng thái không hợp lệ.");
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<TeacherMutationResponse>("Không tìm thấy trung tâm đang hoạt động.");

        var teacher = await _context.Teachers.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.CenterId == centerId && t.UserId == teacherUserId && !t.IsDeleted, cancellationToken);
        if (teacher == null) return Fail<TeacherMutationResponse>("Không tìm thấy hồ sơ giáo viên.");

        teacher.Status = status;
        var membership = await _context.CenterUsers.FirstOrDefaultAsync(cu => cu.CenterId == centerId && cu.UserId == teacherUserId && cu.UserType == "TEACHER", cancellationToken);
        if (membership != null) membership.Status = status;

        if (status == "Active") teacher.User.Status = "Active";
        else
        {
            var otherActive = await _context.CenterUsers.AsNoTracking().AnyAsync(cu => cu.UserId == teacherUserId && cu.Status == "Active" && cu.CenterId != centerId, cancellationToken);
            teacher.User.Status = otherActive ? "Active" : "Inactive";
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ClassOperationResult<TeacherMutationResponse>.Success(new(teacherUserId, false, status), "Đã cập nhật trạng thái giáo viên.");
    }

    public async Task<ClassOperationResult<ResetTeacherPasswordResponse>> ResetPasswordAsync(
        int ownerUserId, int teacherUserId, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<ResetTeacherPasswordResponse>("Không tìm thấy trung tâm đang hoạt động.");

        var teacher = await _context.Teachers.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.CenterId == centerId && t.UserId == teacherUserId && !t.IsDeleted, cancellationToken);
        if (teacher == null) return Fail<ResetTeacherPasswordResponse>("Không tìm thấy hồ sơ giáo viên.");

        var password = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        teacher.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        
        await _context.SaveChangesAsync(cancellationToken);
        return ClassOperationResult<ResetTeacherPasswordResponse>.Success(new(teacherUserId, password), "Đã cấp lại mật khẩu giáo viên.");
    }

    public async Task<ClassOperationResult<bool>> DeleteTeacherAsync(
        int ownerUserId, int teacherUserId, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<bool>("Không tìm thấy trung tâm của bạn.");

        var teacher = await _context.Teachers.Include(t => t.User).Include(t => t.Classes)
            .FirstOrDefaultAsync(t => t.CenterId == centerId && t.UserId == teacherUserId && !t.IsDeleted, cancellationToken);

        if (teacher == null) return Fail<bool>("Không tìm thấy hồ sơ giáo viên.");

        if (teacher.Classes.Any(c => !c.IsDeleted && c.Status != "CANCELLED"))
            return Fail<bool>("Không thể xóa giáo viên đang quản lý lớp học.");

        teacher.Status = "Inactive";
        teacher.IsDeleted = true;
        teacher.User.IsDeleted = true;
        
        var membership = await _context.CenterUsers.FirstOrDefaultAsync(cu => cu.CenterId == centerId && cu.UserId == teacherUserId && cu.UserType == "TEACHER", cancellationToken);
        if (membership != null) membership.Status = "Inactive";

        if (!string.IsNullOrWhiteSpace(teacher.User.AvatarUrl))
        {
            await _storageService.DeleteFileAsync(teacher.User.AvatarUrl, cancellationToken);
            teacher.User.AvatarUrl = null;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ClassOperationResult<bool>.Success(true, "Đã xóa hồ sơ giáo viên.");
    }

    public async Task<ClassOperationResult<string?>> UpdateAvatarAsync(
        int ownerUserId, int teacherUserId, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<string?>("Không tìm thấy trung tâm của bạn.");

        var teacher = await _context.Teachers.Include(t => t.User).FirstOrDefaultAsync(t => t.CenterId == centerId && t.UserId == teacherUserId && !t.IsDeleted, cancellationToken);
        if (teacher == null) return Fail<string?>("Không tìm thấy hồ sơ giáo viên.");

        var oldAvatar = teacher.User.AvatarUrl;
        
        var safeFileName = $"{teacher.TeacherCode.ToLowerInvariant()}-{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        teacher.User.AvatarUrl = await _storageService.SaveFileAsync(fileStream, safeFileName, "teachers", cancellationToken);
        
        await _context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(oldAvatar))
        {
            await _storageService.DeleteFileAsync(oldAvatar, cancellationToken);
        }

        return ClassOperationResult<string?>.Success(teacher.User.AvatarUrl, "Cập nhật ảnh đại diện thành công.");
    }

    public async Task<ClassOperationResult<bool>> RemoveAvatarAsync(
        int ownerUserId, int teacherUserId, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<bool>("Không tìm thấy trung tâm của bạn.");

        var teacher = await _context.Teachers.Include(t => t.User).FirstOrDefaultAsync(t => t.CenterId == centerId && t.UserId == teacherUserId && !t.IsDeleted, cancellationToken);
        if (teacher == null) return Fail<bool>("Không tìm thấy hồ sơ giáo viên.");

        if (!string.IsNullOrWhiteSpace(teacher.User.AvatarUrl))
        {
            await _storageService.DeleteFileAsync(teacher.User.AvatarUrl, cancellationToken);
            teacher.User.AvatarUrl = null;
            await _context.SaveChangesAsync(cancellationToken);
        }
        return ClassOperationResult<bool>.Success(true, "Đã xóa ảnh đại diện.");
    }

    private async Task<int?> GetOwnerCenterIdAsync(int ownerUserId, CancellationToken cancellationToken) =>
        await _context.Centers.AsNoTracking().Where(c => c.OwnerUserId == ownerUserId && c.Status == "Active")
            .Select(c => (int?)c.CenterId).FirstOrDefaultAsync(cancellationToken);

    private static ClassOperationResult<TeacherMutationResponse>? ValidateAndNormalize(SaveTeacherRequest request)
    {
        request.TeacherCode = request.TeacherCode?.Trim().ToUpperInvariant() ?? "";
        request.FullName = Regex.Replace(request.FullName?.Trim() ?? "", @"\s+", " ");
        request.PhoneNumber = request.PhoneNumber?.Trim() ?? "";
        request.Email = NormalizeOptional(request.Email)?.ToLowerInvariant();
        request.Gender = NormalizeOptional(request.Gender) ?? "Nam";
        request.IdentityNumber = new string((request.IdentityNumber ?? "").Where(char.IsDigit).ToArray());
        request.IdentityIssuedPlace = NormalizeOptional(request.IdentityIssuedPlace);
        request.Ethnicity = NormalizeOptional(request.Ethnicity);
        request.Religion = NormalizeOptional(request.Religion);
        request.CurrentAddress = NormalizeOptional(request.CurrentAddress);
        request.PermanentAddress = NormalizeOptional(request.PermanentAddress);
        request.Hometown = NormalizeOptional(request.Hometown);
        request.PlaceOfBirth = NormalizeOptional(request.PlaceOfBirth);
        
        var phone = NormalizePhone(request.PhoneNumber);

        if (string.IsNullOrWhiteSpace(request.TeacherCode)) return Fail<TeacherMutationResponse>("Vui lòng nhập mã giáo viên.", "TeacherCode");
        if (request.FullName.Length < 2) return Fail<TeacherMutationResponse>("Vui lòng nhập tên giáo viên hợp lệ.", "FullName");
        if (phone.Length is < 10 or > 12 || !phone.StartsWith('0')) return Fail<TeacherMutationResponse>("Số điện thoại phải gồm 10-12 số và bắt đầu bằng 0.", "PhoneNumber");
        if (request.Gender is not ("Nam" or "Nữ")) return Fail<TeacherMutationResponse>("Giới tính không hợp lệ.", "Gender");
        if (request.IdentityNumber.Length is not (9 or 12)) return Fail<TeacherMutationResponse>("CMND/CCCD phải gồm 9 hoặc 12 số.", "IdentityNumber");

        return null;
    }

    private static string NormalizePhone(string value)
    {
        var digits = new string((value ?? "").Where(char.IsDigit).ToArray());
        return digits.StartsWith("84") && digits.Length > 9 ? $"0{digits[2..]}" : digits;
    }
    
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : Regex.Replace(value.Trim(), @"\s+", " ");
    
    private static ClassOperationResult<T> Fail<T>(string message, string key = "") =>
        ClassOperationResult<T>.Failure(message, new Dictionary<string, string[]> { [key] = [message] });
}
