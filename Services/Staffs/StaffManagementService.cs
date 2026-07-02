using System.Data;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using EduBridge.Contracts.Staffs;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Services.Classes;
using EduBridge.Services.Storage;
using EduBridge.Services.Auth;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Services.Staffs;

public sealed class StaffManagementService : IStaffManagementService
{
    private static readonly int[] AllowedPageSizes = [10, 20, 50, 100, 200, 500];
    private readonly AppDbContext _context;
    private readonly ILogger<StaffManagementService> _logger;
    private readonly IFileStorageService _storageService;
    private readonly ICurrentCenterService _currentCenterService;

    public StaffManagementService(AppDbContext context, ILogger<StaffManagementService> logger, IFileStorageService storageService, ICurrentCenterService currentCenterService)
    {
        _context = context;
        _logger = logger;
        _storageService = storageService;
        _currentCenterService = currentCenterService;
    }

    public async Task<ClassOperationResult<StaffPagedResponse>> GetStaffsAsync(
        int ownerUserId, StaffQuery query, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<StaffPagedResponse>("Không tìm thấy trung tâm đang hoạt động.");

        query.Page = Math.Max(1, query.Page);
        query.PageSize = AllowedPageSizes.Contains(query.PageSize) ? query.PageSize : 20;
        query.Keyword = NormalizeOptional(query.Keyword);
        query.Status = NormalizeOptional(query.Status);

        if (query.Status is not null and not ("Active" or "Inactive"))
            return Fail<StaffPagedResponse>("Trạng thái lọc không hợp lệ.", "Status");

        var queryable = _context.Users.AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.CenterUsers)
            .Include(u => u.Teacher).ThenInclude(t => t!.Classes).ThenInclude(c => c.Enrollments)
            .Where(u => !u.IsDeleted && u.CenterUsers.Any(cu => cu.CenterId == centerId && (cu.UserType == "TEACHER" || cu.UserType == "OWNER")));

        if (query.Keyword != null)
        {
            var phone = NormalizePhone(query.Keyword);
            var isPhoneSearch = !string.IsNullOrEmpty(phone) && !query.Keyword.Any(char.IsLetter);
            queryable = queryable.Where(u => 
                (u.CenterUsers.Any(cu => cu.CenterId == centerId && cu.StaffCode != null && cu.StaffCode.Contains(query.Keyword))) ||
                (u.Teacher != null && u.Teacher.CenterId == centerId && u.Teacher.TeacherCode.Contains(query.Keyword)) || 
                u.FullName.Contains(query.Keyword) || 
                (u.PhoneNumber != null && u.PhoneNumber.Contains(query.Keyword)) ||
                (isPhoneSearch && u.NormalizedPhoneNumber != null && u.NormalizedPhoneNumber.Contains(phone)));
        }

        if (query.Status != null) queryable = queryable.Where(u => u.CenterUsers.Any(cu => cu.CenterId == centerId && cu.Status == query.Status));
        if (query.Role != null) queryable = queryable.Where(u => u.CenterUsers.Any(cu => cu.CenterId == centerId && cu.UserType == query.Role));

        var total = await queryable.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)query.PageSize));
        query.Page = Math.Min(query.Page, totalPages);

        var items = await queryable
            .OrderByDescending(u => u.UserId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new 
            {
                User = u,
                CenterUsers = u.CenterUsers.Where(cu => cu.CenterId == centerId && (cu.UserType == "TEACHER" || cu.UserType == "OWNER")).ToList(),
                Teacher = u.Teacher != null && u.Teacher.CenterId == centerId && !u.Teacher.IsDeleted ? u.Teacher : null
            })
            .ToListAsync(cancellationToken);

        var responseItems = items.Select(x => {
            var primaryCu = x.CenterUsers.FirstOrDefault(c => c.UserType == "OWNER") ?? x.CenterUsers.FirstOrDefault();
            var staffCode = primaryCu?.StaffCode;
            if (string.IsNullOrEmpty(staffCode) && x.Teacher != null) staffCode = x.Teacher.TeacherCode;
            if (string.IsNullOrEmpty(staffCode)) staffCode = "-";

            return new StaffListItemResponse(
                x.User.UserId, 
                staffCode, 
                x.User.FullName, 
                x.CenterUsers.Select(c => c.UserType).ToList(),
                x.User.PhoneNumber, x.User.Email, x.User.AvatarUrl,
                x.Teacher?.Specialization,
                x.Teacher != null ? x.Teacher.Classes.Count(c => !c.IsDeleted && c.Status != "CANCELLED") : 0,
                x.Teacher != null ? x.Teacher.Classes.Where(c => !c.IsDeleted && c.Status != "CANCELLED").SelectMany(c => c.Enrollments).Count(e => e.Status == "Đang học") : 0,
                primaryCu?.Status ?? "Inactive", x.User.CreatedAt);
        }).ToList();

        return ClassOperationResult<StaffPagedResponse>.Success(
            new StaffPagedResponse(responseItems, query.Page, query.PageSize, total, totalPages), "Tải danh sách nhân sự thành công.");
    }

    public async Task<ClassOperationResult<StaffDetailResponse>> GetStaffAsync(
        int ownerUserId, int staffUserId, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<StaffDetailResponse>("Không tìm thấy trung tâm đang hoạt động.");

        var staff = await _context.Users.AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.CenterUsers)
            .Include(u => u.Teacher)
            .Where(u => !u.IsDeleted && u.UserId == staffUserId && u.CenterUsers.Any(cu => cu.CenterId == centerId && (cu.UserType == "TEACHER" || cu.UserType == "OWNER")))
            .Select(u => new 
            {
                User = u,
                CenterUsers = u.CenterUsers.Where(cu => cu.CenterId == centerId && (cu.UserType == "TEACHER" || cu.UserType == "OWNER")).ToList(),
                Teacher = u.Teacher != null && u.Teacher.CenterId == centerId && !u.Teacher.IsDeleted ? u.Teacher : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (staff == null) return Fail<StaffDetailResponse>("Không tìm thấy nhân sự trong trung tâm.");

        var primaryCu = staff.CenterUsers.FirstOrDefault(c => c.UserType == "OWNER") ?? staff.CenterUsers.FirstOrDefault();
        var staffCode = primaryCu?.StaffCode;
        if (string.IsNullOrEmpty(staffCode) && staff.Teacher != null) staffCode = staff.Teacher.TeacherCode;

        var detail = new StaffDetailResponse(
            staff.User.UserId, staffCode ?? string.Empty, 
            staff.User.FullName, staff.CenterUsers.Select(c => c.UserType).ToList(), staff.User.PhoneNumber, staff.User.Email, staff.User.AvatarUrl,
            staff.Teacher?.Specialization,
            staff.Teacher?.ExperienceYears ?? 0,
            staff.User.DateOfBirth, staff.User.Gender ?? string.Empty, staff.User.Ethnicity, staff.User.Religion, staff.User.IdentityNumber ?? string.Empty,
            staff.User.IdentityIssuedDate, staff.User.IdentityIssuedPlace, staff.User.CurrentAddress, staff.User.PermanentAddress,
            staff.User.Hometown, staff.User.PlaceOfBirth, primaryCu?.Status ?? "Inactive", staff.User.CreatedAt);

        return ClassOperationResult<StaffDetailResponse>.Success(detail, "Tải thông tin nhân sự thành công.");
    }

    public async Task<ClassOperationResult<StaffMutationResponse>> CreateAsync(
        int ownerUserId, SaveStaffRequest request, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<StaffMutationResponse>("Không tìm thấy trung tâm đang hoạt động.");

        var validation = ValidateAndNormalize(request);
        if (validation != null) return validation;

        var staffCodeExists = await _context.CenterUsers.AsNoTracking().AnyAsync(
            cu => cu.CenterId == centerId && cu.StaffCode == request.StaffCode, cancellationToken);
        if (staffCodeExists) return Fail<StaffMutationResponse>("Mã nhân sự đã tồn tại.", "StaffCode");

        if (request.Roles.Contains("TEACHER"))
        {
            var teacherCodeExists = await _context.Teachers.AsNoTracking().AnyAsync(
                t => t.CenterId == centerId && !t.IsDeleted && t.TeacherCode == request.StaffCode, cancellationToken);
            if (teacherCodeExists) return Fail<StaffMutationResponse>("Mã nhân sự (giáo viên) đã tồn tại.", "StaffCode");
        }

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            
            var phone = NormalizePhone(request.PhoneNumber);
            var existingByPhone = string.IsNullOrEmpty(phone) ? null : await _context.Users.Include(u => u.Role).Include(u => u.CenterUsers)
                .FirstOrDefaultAsync(u => !u.IsDeleted && u.NormalizedPhoneNumber == phone, cancellationToken);
            var existingByEmail = string.IsNullOrEmpty(request.Email) ? null : await _context.Users.Include(u => u.Role).Include(u => u.CenterUsers)
                .FirstOrDefaultAsync(u => !u.IsDeleted && u.Email == request.Email, cancellationToken);
            var existingByIdentity = string.IsNullOrEmpty(request.IdentityNumber) ? null : await _context.Users.Include(u => u.Role).Include(u => u.CenterUsers)
                .FirstOrDefaultAsync(u => !u.IsDeleted && u.IdentityNumber == request.IdentityNumber, cancellationToken);

            var duplicateUserIds = new[] { existingByPhone?.UserId, existingByEmail?.UserId, existingByIdentity?.UserId }
                .Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();

            if (duplicateUserIds.Count > 1)
                return Fail<StaffMutationResponse>("Số điện thoại, Email hoặc CMND/CCCD đang thuộc về các tài khoản khác nhau.");

            var existing = existingByPhone ?? existingByEmail ?? existingByIdentity;

            if (existing != null)
            {
                var existingRolesInCenter = existing.CenterUsers.Where(cu => cu.CenterId == centerId).Select(cu => cu.UserType).ToList();
                if (request.Roles.Any(r => existingRolesInCenter.Contains(r)))
                {
                    if (existingByIdentity != null)
                        return Fail<StaffMutationResponse>("CMND/CCCD đã tồn tại", "IdentityNumber");
                        
                    if (existingByPhone != null)
                        return Fail<StaffMutationResponse>("Số điện thoại đã tồn tại", "PhoneNumber");
                        
                    if (existingByEmail != null)
                        return Fail<StaffMutationResponse>("Email đã tồn tại", "Email");
                        
                    return Fail<StaffMutationResponse>("Nhân sự này đã tồn tại trong trung tâm.");
                }
            }
            var created = false;
            
            // Lấy role ưu tiên làm RoleId hệ thống (OWNER > TEACHER)
            var primaryRole = request.Roles.Contains("OWNER") ? "OWNER" : request.Roles.First();

            if (existing == null)
            {
                var roleId = await _context.Roles.Where(r => r.RoleCode == primaryRole).Select(r => (int?)r.RoleId).FirstOrDefaultAsync(cancellationToken);
                if (roleId == null) return Fail<StaffMutationResponse>($"Database chưa có role {primaryRole}.");

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
            else
            {
                // Cập nhật lại RoleId hệ thống nếu tài khoản đã tồn tại và role mới có quyền cao hơn (vd trước là TEACHER, nay thêm OWNER)
                if (existing.Role.RoleCode != "OWNER" && request.Roles.Contains("OWNER"))
                {
                    var ownerRoleId = await _context.Roles.Where(r => r.RoleCode == "OWNER").Select(r => (int?)r.RoleId).FirstOrDefaultAsync(cancellationToken);
                    if (ownerRoleId != null) existing.RoleId = ownerRoleId.Value;
                }
            }

            foreach (var role in request.Roles)
            {
                var membership = await _context.CenterUsers.FirstOrDefaultAsync(cu =>
                    cu.CenterId == centerId && cu.UserId == existing.UserId && cu.UserType == role, cancellationToken);
                
                if (membership == null)
                {
                    membership = new CenterUser
                    {
                        CenterId = centerId.Value, UserId = existing.UserId, UserType = role,
                        Status = request.IsActive ? "Active" : "Inactive", CreatedAt = DateTime.UtcNow,
                        StaffCode = request.StaffCode
                    };
                    _context.CenterUsers.Add(membership);
                }
                else
                {
                    membership.StaffCode = request.StaffCode;
                    membership.Status = request.IsActive ? "Active" : "Inactive";
                }
            }

            if (request.Roles.Contains("TEACHER"))
            {
                var existingTeacher = await _context.Teachers.FirstOrDefaultAsync(t =>
                    t.CenterId == centerId && t.UserId == existing.UserId && !t.IsDeleted, cancellationToken);

                if (existingTeacher == null)
                {
                    var newTeacher = new Teacher
                    {
                        CenterId = centerId.Value, UserId = existing.UserId, TeacherCode = request.StaffCode,
                        Specialization = request.Specialization, ExperienceYears = request.ExperienceYears ?? 0,
                        Status = request.IsActive ? "Active" : "Inactive", IsDeleted = false
                    };
                    _context.Teachers.Add(newTeacher);
                }
                else
                {
                    // Update current teacher record if it exists
                    existingTeacher.TeacherCode = request.StaffCode;
                    existingTeacher.Specialization = request.Specialization;
                    existingTeacher.ExperienceYears = request.ExperienceYears ?? 0;
                    existingTeacher.Status = request.IsActive ? "Active" : "Inactive";
                }
            }

            if (request.IsActive) existing.Status = "Active";

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            return ClassOperationResult<StaffMutationResponse>.Success(
                new StaffMutationResponse(existing.UserId, created, request.IsActive ? "Active" : "Inactive"),
                created ? "Đã tạo hồ sơ nhân sự mới." : "Đã cập nhật/thêm hồ sơ nhân sự vào trung tâm.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Could not create staff.");
            var msg = ex.InnerException?.Message ?? ex.Message;
            if (msg.Contains("UX_Users_Email_NotNull")) return Fail<StaffMutationResponse>("Email này đã được sử dụng trong hệ thống.", "Email");
            if (msg.Contains("UX_Users_NormalizedPhoneNumber_NotNull")) return Fail<StaffMutationResponse>("Số điện thoại này đã được sử dụng trong hệ thống.", "PhoneNumber");
            if (msg.Contains("UX_Users_IdentityNumber_NotNull")) return Fail<StaffMutationResponse>("CMND/CCCD này đã được sử dụng trong hệ thống.", "IdentityNumber");
            
            return Fail<StaffMutationResponse>("Không thể lưu thông tin do dữ liệu trùng hoặc vừa thay đổi.");
        }
    }

    public async Task<ClassOperationResult<StaffMutationResponse>> UpdateAsync(
        int ownerUserId, int staffUserId, SaveStaffRequest request, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<StaffMutationResponse>("Không tìm thấy trung tâm đang hoạt động.");

        var validation = ValidateAndNormalize(request);
        if (validation != null) return validation;

        var existingUser = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.CenterUsers)
            .Include(u => u.Teacher)
            .FirstOrDefaultAsync(u => u.UserId == staffUserId && !u.IsDeleted, cancellationToken);
        
        if (existingUser == null) return Fail<StaffMutationResponse>("Không tìm thấy hồ sơ nhân sự.");

        var existingMemberships = existingUser.CenterUsers.Where(cu => cu.CenterId == centerId && (cu.UserType == "TEACHER" || cu.UserType == "OWNER")).ToList();
        if (!existingMemberships.Any()) return Fail<StaffMutationResponse>("Tài khoản không thuộc trung tâm hoặc không phải nhân sự.");

        var staffCodeExists = await _context.CenterUsers.AsNoTracking().AnyAsync(
            cu => cu.CenterId == centerId && cu.StaffCode == request.StaffCode && cu.UserId != staffUserId, cancellationToken);
        if (staffCodeExists) return Fail<StaffMutationResponse>("Mã nhân sự đã tồn tại.", "StaffCode");

        if (request.Roles.Contains("TEACHER"))
        {
            var teacherCodeExists = await _context.Teachers.AsNoTracking().AnyAsync(
                t => t.CenterId == centerId && !t.IsDeleted && t.TeacherCode == request.StaffCode && t.UserId != staffUserId, cancellationToken);
            if (teacherCodeExists) return Fail<StaffMutationResponse>("Mã nhân sự (giáo viên) đã tồn tại.", "StaffCode");
        }

        var phone = NormalizePhone(request.PhoneNumber);
        var duplicateUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => !u.IsDeleted && u.UserId != staffUserId &&
            (u.NormalizedPhoneNumber == phone || (request.Email != null && u.Email == request.Email) ||
             (request.IdentityNumber != null && u.IdentityNumber == request.IdentityNumber)), cancellationToken);

        if (duplicateUser != null)
        {
            if (duplicateUser.NormalizedPhoneNumber == phone) return Fail<StaffMutationResponse>("Số điện thoại đã tồn tại.", "PhoneNumber");
            if (request.Email != null && duplicateUser.Email == request.Email) return Fail<StaffMutationResponse>("Email đã tồn tại.", "Email");
            if (request.IdentityNumber != null && duplicateUser.IdentityNumber == request.IdentityNumber) return Fail<StaffMutationResponse>("CCCD/CMND đã tồn tại.", "IdentityNumber");
        }

        // Handle Role Updates
        var currentRoles = existingMemberships.Select(m => m.UserType).ToList();
        var rolesToAdd = request.Roles.Except(currentRoles).ToList();
        var rolesToRemove = currentRoles.Except(request.Roles).ToList();

        // Remove unselected roles
        foreach (var role in rolesToRemove)
        {
            var membershipToRemove = existingMemberships.First(m => m.UserType == role);
            _context.CenterUsers.Remove(membershipToRemove);
            
            // If TEACHER is removed, mark Teacher record as deleted
            if (role == "TEACHER" && existingUser.Teacher != null && existingUser.Teacher.CenterId == centerId)
            {
                // Allow removing TEACHER role ONLY if they aren't assigned to active classes
                var hasActiveClasses = await _context.Classes.AnyAsync(c => c.TeacherId == existingUser.Teacher.TeacherId && !c.IsDeleted && c.Status != "CANCELLED", cancellationToken);
                if (hasActiveClasses) return Fail<StaffMutationResponse>("Không thể gỡ vai trò Giáo viên vì giáo viên này đang phụ trách lớp học.");
                
                existingUser.Teacher.IsDeleted = true;
                existingUser.Teacher.Status = "Inactive";
            }
        }

        // Add newly selected roles
        foreach (var role in rolesToAdd)
        {
            var newMembership = new CenterUser
            {
                CenterId = centerId.Value, UserId = staffUserId, UserType = role,
                Status = request.IsActive ? "Active" : "Inactive", CreatedAt = DateTime.UtcNow,
                StaffCode = request.StaffCode
            };
            _context.CenterUsers.Add(newMembership);
        }

        // Update existing roles
        var rolesToKeep = currentRoles.Intersect(request.Roles).ToList();
        foreach (var role in rolesToKeep)
        {
            var membershipToKeep = existingMemberships.First(m => m.UserType == role);
            membershipToKeep.StaffCode = request.StaffCode;
            membershipToKeep.Status = request.IsActive ? "Active" : "Inactive";
        }

        if (request.Roles.Contains("TEACHER"))
        {
            if (existingUser.Teacher == null)
            {
                var newTeacher = new Teacher
                {
                    CenterId = centerId.Value, UserId = staffUserId, TeacherCode = request.StaffCode,
                    Specialization = request.Specialization, ExperienceYears = request.ExperienceYears ?? 0,
                    Status = request.IsActive ? "Active" : "Inactive", IsDeleted = false
                };
                _context.Teachers.Add(newTeacher);
            }
            else
            {
                existingUser.Teacher.TeacherCode = request.StaffCode;
                existingUser.Teacher.Specialization = request.Specialization;
                existingUser.Teacher.ExperienceYears = request.ExperienceYears ?? 0;
                existingUser.Teacher.Status = request.IsActive ? "Active" : "Inactive";
                existingUser.Teacher.IsDeleted = false;
            }
        }

        // Update System RoleId if OWNER is added
        if (existingUser.Role.RoleCode != "OWNER" && request.Roles.Contains("OWNER"))
        {
            var ownerRoleId = await _context.Roles.Where(r => r.RoleCode == "OWNER").Select(r => (int?)r.RoleId).FirstOrDefaultAsync(cancellationToken);
            if (ownerRoleId != null) existingUser.RoleId = ownerRoleId.Value;
        }
        else if (existingUser.Role.RoleCode == "OWNER" && !request.Roles.Contains("OWNER") && request.Roles.Contains("TEACHER"))
        {
            var teacherRoleId = await _context.Roles.Where(r => r.RoleCode == "TEACHER").Select(r => (int?)r.RoleId).FirstOrDefaultAsync(cancellationToken);
            if (teacherRoleId != null) existingUser.RoleId = teacherRoleId.Value;
        }

        existingUser.FullName = request.FullName;
        existingUser.PhoneNumber = request.PhoneNumber;
        existingUser.NormalizedPhoneNumber = phone;
        existingUser.Email = request.Email;
        existingUser.DateOfBirth = request.DateOfBirth;
        existingUser.Gender = request.Gender;
        existingUser.IdentityNumber = request.IdentityNumber;
        existingUser.IdentityIssuedDate = request.IdentityIssuedDate;
        existingUser.IdentityIssuedPlace = request.IdentityIssuedPlace;
        existingUser.Ethnicity = request.Ethnicity;
        existingUser.Religion = request.Religion;
        existingUser.CurrentAddress = request.CurrentAddress;
        existingUser.PermanentAddress = request.PermanentAddress;
        existingUser.Hometown = request.Hometown;
        existingUser.PlaceOfBirth = request.PlaceOfBirth;

        if (request.IsActive) existingUser.Status = "Active";
        else
        {
            var otherActive = await _context.CenterUsers.AsNoTracking().AnyAsync(cu => cu.UserId == staffUserId && cu.Status == "Active" && cu.CenterId != centerId, cancellationToken);
            existingUser.Status = otherActive ? "Active" : "Inactive";
        }

        try 
        {
            await _context.SaveChangesAsync(cancellationToken);
            return ClassOperationResult<StaffMutationResponse>.Success(new(staffUserId, false, request.IsActive ? "Active" : "Inactive"), "Cập nhật hồ sơ nhân sự thành công.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Could not update staff.");
            var msg = ex.InnerException?.Message ?? ex.Message;
            if (msg.Contains("UX_Users_Email_NotNull")) return Fail<StaffMutationResponse>("Email này đã được sử dụng trong hệ thống.", "Email");
            if (msg.Contains("UX_Users_NormalizedPhoneNumber_NotNull")) return Fail<StaffMutationResponse>("Số điện thoại này đã được sử dụng trong hệ thống.", "PhoneNumber");
            if (msg.Contains("UX_Users_IdentityNumber_NotNull")) return Fail<StaffMutationResponse>("CMND/CCCD này đã được sử dụng trong hệ thống.", "IdentityNumber");
            
            return Fail<StaffMutationResponse>("Không thể lưu thông tin do dữ liệu trùng hoặc vừa thay đổi.");
        }
    }

    public async Task<ClassOperationResult<StaffMutationResponse>> SetStatusAsync(
        int ownerUserId, int staffUserId, string status, CancellationToken cancellationToken = default)
    {
        if (status is not ("Active" or "Inactive")) return Fail<StaffMutationResponse>("Trạng thái không hợp lệ.");
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<StaffMutationResponse>("Không tìm thấy trung tâm đang hoạt động.");

        var memberships = await _context.CenterUsers.Include(cu => cu.User).ThenInclude(u => u.Teacher)
            .Where(cu => cu.CenterId == centerId && cu.UserId == staffUserId && (cu.UserType == "TEACHER" || cu.UserType == "OWNER") && !cu.User.IsDeleted).ToListAsync(cancellationToken);
        
        if (!memberships.Any()) return Fail<StaffMutationResponse>("Không tìm thấy hồ sơ nhân sự.");

        foreach(var membership in memberships)
        {
            membership.Status = status;
        }

        var primaryUser = memberships.First().User;
        if (primaryUser.Teacher != null) primaryUser.Teacher.Status = status;

        if (status == "Active") primaryUser.Status = "Active";
        else
        {
            var otherActive = await _context.CenterUsers.AsNoTracking().AnyAsync(cu => cu.UserId == staffUserId && cu.Status == "Active" && cu.CenterId != centerId, cancellationToken);
            primaryUser.Status = otherActive ? "Active" : "Inactive";
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ClassOperationResult<StaffMutationResponse>.Success(new(staffUserId, false, status), "Đã cập nhật trạng thái nhân sự.");
    }

    public async Task<ClassOperationResult<ResetStaffPasswordResponse>> ResetPasswordAsync(
        int ownerUserId, int staffUserId, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<ResetStaffPasswordResponse>("Không tìm thấy trung tâm đang hoạt động.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == staffUserId && !u.IsDeleted && u.CenterUsers.Any(cu => cu.CenterId == centerId && (cu.UserType == "TEACHER" || cu.UserType == "OWNER")), cancellationToken);
        if (user == null) return Fail<ResetStaffPasswordResponse>("Không tìm thấy hồ sơ nhân sự.");

        var password = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        
        await _context.SaveChangesAsync(cancellationToken);
        return ClassOperationResult<ResetStaffPasswordResponse>.Success(new(staffUserId, password), "Đã cấp lại mật khẩu nhân sự.");
    }

    public async Task<ClassOperationResult<bool>> DeleteStaffAsync(
        int ownerUserId, int staffUserId, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<bool>("Không tìm thấy trung tâm của bạn.");

        var memberships = await _context.CenterUsers.Include(cu => cu.User).ThenInclude(u => u.Teacher).ThenInclude(t => t!.Classes)
            .Where(cu => cu.CenterId == centerId && cu.UserId == staffUserId && (cu.UserType == "TEACHER" || cu.UserType == "OWNER") && !cu.User.IsDeleted).ToListAsync(cancellationToken);

        if (!memberships.Any()) return Fail<bool>("Không tìm thấy hồ sơ nhân sự.");

        var primaryUser = memberships.First().User;

        if (memberships.Any(m => m.UserType == "TEACHER") && primaryUser.Teacher != null && primaryUser.Teacher.Classes.Any(c => !c.IsDeleted && c.Status != "CANCELLED"))
            return Fail<bool>("Không thể xóa giáo viên đang quản lý lớp học.");

        if (memberships.Any(m => m.UserType == "OWNER") && staffUserId == ownerUserId)
            return Fail<bool>("Không thể tự xóa tài khoản của chính mình.");

        foreach(var membership in memberships)
        {
            membership.Status = "Inactive";
        }
        
        primaryUser.IsDeleted = true;
        
        if (primaryUser.Teacher != null)
        {
            primaryUser.Teacher.Status = "Inactive";
            primaryUser.Teacher.IsDeleted = true;
        }

        if (!string.IsNullOrWhiteSpace(primaryUser.AvatarUrl))
        {
            await _storageService.DeleteFileAsync(primaryUser.AvatarUrl, cancellationToken);
            primaryUser.AvatarUrl = null;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ClassOperationResult<bool>.Success(true, "Đã xóa hồ sơ nhân sự.");
    }

    public async Task<ClassOperationResult<string?>> UpdateAvatarAsync(
        int ownerUserId, int staffUserId, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<string?>("Không tìm thấy trung tâm của bạn.");

        var user = await _context.Users.Include(u => u.Teacher).FirstOrDefaultAsync(u => !u.IsDeleted && u.UserId == staffUserId && u.CenterUsers.Any(cu => cu.CenterId == centerId), cancellationToken);
        if (user == null) return Fail<string?>("Không tìm thấy hồ sơ nhân sự.");

        var oldAvatar = user.AvatarUrl;
        
        var prefix = user.Teacher != null ? user.Teacher.TeacherCode.ToLowerInvariant() : user.UserId.ToString();
        var safeFileName = $"{prefix}-{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        user.AvatarUrl = await _storageService.SaveFileAsync(fileStream, safeFileName, "staffs", cancellationToken);
        
        await _context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(oldAvatar))
        {
            await _storageService.DeleteFileAsync(oldAvatar, cancellationToken);
        }

        return ClassOperationResult<string?>.Success(user.AvatarUrl, "Cập nhật ảnh đại diện thành công.");
    }

    public async Task<ClassOperationResult<bool>> RemoveAvatarAsync(
        int ownerUserId, int staffUserId, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<bool>("Không tìm thấy trung tâm của bạn.");

        var user = await _context.Users.FirstOrDefaultAsync(u => !u.IsDeleted && u.UserId == staffUserId && u.CenterUsers.Any(cu => cu.CenterId == centerId), cancellationToken);
        if (user == null) return Fail<bool>("Không tìm thấy hồ sơ nhân sự.");

        if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            await _storageService.DeleteFileAsync(user.AvatarUrl, cancellationToken);
            user.AvatarUrl = null;
            await _context.SaveChangesAsync(cancellationToken);
        }
        return ClassOperationResult<bool>.Success(true, "Đã xóa ảnh đại diện.");
    }

    private async Task<int?> GetOwnerCenterIdAsync(int ownerUserId, CancellationToken cancellationToken) =>
        await _currentCenterService.GetCenterIdAsync(cancellationToken);

    private static ClassOperationResult<StaffMutationResponse>? ValidateAndNormalize(SaveStaffRequest request)
    {
        if (request.Roles == null || !request.Roles.Any()) return Fail<StaffMutationResponse>("Vui lòng chọn ít nhất 1 vai trò.", "Roles");
        request.Roles = request.Roles.Select(r => r.Trim().ToUpperInvariant()).Distinct().ToList();
        if (request.Roles.Any(r => r is not ("TEACHER" or "OWNER"))) return Fail<StaffMutationResponse>("Vai trò không hợp lệ.", "Roles");

        request.StaffCode = request.StaffCode?.Trim().ToUpperInvariant() ?? "";
        if (string.IsNullOrWhiteSpace(request.StaffCode)) return Fail<StaffMutationResponse>("Vui lòng nhập mã nhân sự.", "StaffCode");

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
        request.Specialization = NormalizeOptional(request.Specialization);
        
        var phone = NormalizePhone(request.PhoneNumber);

        if (request.FullName.Length < 2) return Fail<StaffMutationResponse>("Vui lòng nhập tên nhân sự hợp lệ.", "FullName");
        
        if (!string.IsNullOrEmpty(phone))
        {
            if (phone.Length is < 10 or > 12 || !phone.StartsWith('0')) 
                return Fail<StaffMutationResponse>("Số điện thoại phải gồm 10-12 số và bắt đầu bằng 0.", "PhoneNumber");
        }

        if (request.Gender is not ("Nam" or "Nữ")) return Fail<StaffMutationResponse>("Giới tính không hợp lệ.", "Gender");
        
        if (!string.IsNullOrEmpty(request.IdentityNumber))
        {
            if (request.IdentityNumber.Length is not (9 or 12)) 
                return Fail<StaffMutationResponse>("CMND/CCCD phải gồm 9 hoặc 12 số.", "IdentityNumber");
        }

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
