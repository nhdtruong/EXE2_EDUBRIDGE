using System.Data;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
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
    private readonly EduBridge.Services.ImportExportHistories.IImportExportHistoryService _historyService;

    public StaffManagementService(
        AppDbContext context, 
        ILogger<StaffManagementService> logger, 
        IFileStorageService storageService, 
        ICurrentCenterService currentCenterService,
        EduBridge.Services.ImportExportHistories.IImportExportHistoryService historyService)
    {
        _context = context;
        _logger = logger;
        _storageService = storageService;
        _currentCenterService = currentCenterService;
        _historyService = historyService;
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

    public async Task<ClassOperationResult<EduBridge.Contracts.Students.ImportResultResponse>> ImportStaffsFromExcelAsync(int ownerUserId, Microsoft.AspNetCore.Http.IFormFile importFile, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return ClassOperationResult<EduBridge.Contracts.Students.ImportResultResponse>.Failure("Không tìm thấy trung tâm.");

        var response = new EduBridge.Contracts.Students.ImportResultResponse();
        var now = DateTime.UtcNow;
        var teacherRoleId = await _context.Roles.Where(r => r.RoleCode == "TEACHER").Select(r => (int?)r.RoleId).FirstOrDefaultAsync(cancellationToken);
        if (teacherRoleId == null) return ClassOperationResult<EduBridge.Contracts.Students.ImportResultResponse>.Failure("Hệ thống chưa cấu hình role TEACHER.");

        var inputFileUrl = await _storageService.SaveFileAsync(importFile, "imports", cancellationToken);

        var history = await _historyService.CreateHistoryAsync(new EduBridge.Contracts.ImportExportHistories.CreateImportExportHistoryRequest
        {
            CenterId = centerId.Value,
            UserId = ownerUserId,
            Title = $"Import nhân sự {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
            ActionType = "Import",
            EntityName = "Staffs",
            InputFileUrl = inputFileUrl,
            Status = "Processing"
        }, cancellationToken);

        string? resultFileUrl = null;
        try
        {
            using var excelStream = importFile.OpenReadStream();
            using var excelMemoryStream = new MemoryStream();
            await excelStream.CopyToAsync(excelMemoryStream, cancellationToken);
            excelMemoryStream.Position = 0;
            
            using var workbook = new XLWorkbook(excelMemoryStream);
            var worksheet = workbook.Worksheet(1);
            
            var rangeUsed = worksheet.RangeUsed();
            if (rangeUsed == null)
            {
                return ClassOperationResult<EduBridge.Contracts.Students.ImportResultResponse>.Failure("File Excel không có dữ liệu.");
            }
            var rows = rangeUsed.RowsUsed().Skip(1).ToList();

            var phoneNumbersInExcel = new HashSet<string>();
            var emailsInExcel = new HashSet<string>();
            foreach (var row in rows)
            {
                var phone = row.Cell(8).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(phone))
                {
                    var normPhone = string.Concat(phone.Where(char.IsDigit));
                    if (normPhone.StartsWith("84")) normPhone = "0" + normPhone.Substring(2);
                    if (normPhone.StartsWith("+84")) normPhone = "0" + normPhone.Substring(3);
                    phoneNumbersInExcel.Add(normPhone);
                }

                var email = row.Cell(9).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(email))
                {
                    emailsInExcel.Add(email.ToLower());
                }
            }

            var phoneNumbersList = phoneNumbersInExcel.ToList();
            var existingUsersByPhone = await _context.Users
                .Include(u => u.CenterUsers)
                .Where(u => !u.IsDeleted && u.NormalizedPhoneNumber != null && phoneNumbersList.Contains(u.NormalizedPhoneNumber))
                .ToListAsync(cancellationToken);

            var emailsList = emailsInExcel.ToList();
            var existingUsersByEmail = await _context.Users
                .Include(u => u.CenterUsers)
                .Where(u => !u.IsDeleted && u.Email != null && emailsList.Contains(u.Email))
                .ToListAsync(cancellationToken);

            var existingUserPhones = existingUsersByPhone
                .Where(u => !string.IsNullOrEmpty(u.NormalizedPhoneNumber))
                .GroupBy(u => u.NormalizedPhoneNumber!)
                .ToDictionary(g => g.Key, g => g.First());
                
            var existingUserEmails = existingUsersByEmail
                .Where(u => !string.IsNullOrEmpty(u.Email))
                .GroupBy(u => u.Email!.ToLower())
                .ToDictionary(g => g.Key, g => g.First());

            var existingCenterUsers = await _context.CenterUsers
                .Where(cu => cu.CenterId == centerId)
                .ToListAsync(cancellationToken);
                
            var existingCenterUsersByStaffCode = existingCenterUsers
                .Where(cu => !string.IsNullOrEmpty(cu.StaffCode))
                .GroupBy(cu => cu.StaffCode!.ToLower())
                .ToDictionary(g => g.Key, g => g.First());

            int actualLastRow = 1;
            foreach (var row in rows)
            {
                response.TotalRows++;
                var rowIndex = row.RowNumber();
                bool isEmptyRow = false;
                int initialErrorCount = response.ErrorCount;
                
                try
                {
                    var sttStr = row.Cell(1).GetString().Trim();
                    var idStr = row.Cell(2).GetString().Trim();
                    var staffCode = row.Cell(3).GetString().Trim();
                    var fullName = row.Cell(4).GetString().Trim();
                    var dobStr = row.Cell(5).GetString().Trim();
                    var genderStr = row.Cell(6).GetString().Trim();
                    var identityStr = row.Cell(7).GetString().Trim();
                    var phoneStr = row.Cell(8).GetString().Trim();
                    var emailStr = row.Cell(9).GetString().Trim();
                    var specStr = row.Cell(10).GetString().Trim();
                    var expStr = row.Cell(11).GetString().Trim();

                    var rowPrefix = string.IsNullOrWhiteSpace(sttStr) ? $"Dòng {rowIndex}" : $"STT {sttStr}";

                    if (string.IsNullOrWhiteSpace(sttStr) && string.IsNullOrWhiteSpace(staffCode) && 
                        string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(phoneStr))
                    {
                        isEmptyRow = true;
                        response.TotalRows--;
                        continue;
                    }
                    
                    int updateUserId = 0;
                    bool isUpdate = false;
                    if (!string.IsNullOrWhiteSpace(idStr))
                    {
                        if (int.TryParse(idStr, out updateUserId) && updateUserId > 0)
                        {
                            isUpdate = true;
                        }
                        else
                        {
                            response.Errors.Add($"{rowPrefix}: ID không hợp lệ.");
                            response.ErrorCount++;
                            continue;
                        }
                    }

                    if (!isUpdate)
                    {
                        var missingFields = new List<string>();
                        if (string.IsNullOrWhiteSpace(sttStr)) missingFields.Add("STT");
                        if (string.IsNullOrWhiteSpace(staffCode)) missingFields.Add("Mã nhân sự");
                        if (string.IsNullOrWhiteSpace(fullName)) missingFields.Add("Họ và tên");
                        if (string.IsNullOrWhiteSpace(dobStr)) missingFields.Add("Ngày sinh");
                        if (string.IsNullOrWhiteSpace(genderStr)) missingFields.Add("Giới tính");
                        if (string.IsNullOrWhiteSpace(phoneStr)) missingFields.Add("SĐT");

                        if (missingFields.Any())
                        {
                            response.Errors.Add($"{rowPrefix}: Thiếu thông tin bắt buộc ({string.Join(", ", missingFields)}).");
                            response.ErrorCount++;
                            continue;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(staffCode) && !Regex.IsMatch(staffCode, @"^[A-Za-z0-9][A-Za-z0-9\-_]{1,29}$"))
                    {
                        response.Errors.Add($"{rowPrefix}: Mã nhân sự không hợp lệ.");
                        response.ErrorCount++;
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(staffCode) && existingCenterUsersByStaffCode.TryGetValue(staffCode.ToLower(), out var cuCode) && (!isUpdate || cuCode.UserId != updateUserId))
                    {
                        response.Errors.Add($"{rowPrefix}: Mã nhân sự đã tồn tại trong trung tâm.");
                        response.ErrorCount++;
                        continue;
                    }

                    string? gender = null;
                    if (!string.IsNullOrWhiteSpace(genderStr))
                    {
                        if (genderStr.Equals("1") || genderStr.Equals("Nam", StringComparison.OrdinalIgnoreCase)) gender = "Nam";
                        else if (genderStr.Equals("2") || genderStr.Equals("Nữ", StringComparison.OrdinalIgnoreCase)) gender = "Nữ";
                        else
                        {
                            response.Errors.Add($"{rowPrefix}: Giới tính không hợp lệ (1/Nam hoặc 2/Nữ).");
                            response.ErrorCount++;
                            continue;
                        }
                    }

                    DateOnly? dateOfBirth = null;
                    if (!string.IsNullOrWhiteSpace(dobStr))
                    {
                        if (!DateOnly.TryParseExact(dobStr, new[] { "dd/MM/yyyy", "d/M/yyyy" }, null, System.Globalization.DateTimeStyles.None, out var dob))
                        {
                            response.Errors.Add($"{rowPrefix}: Ngày sinh không hợp lệ (dd/MM/yyyy).");
                            response.ErrorCount++;
                            continue;
                        }
                        dateOfBirth = dob;
                    }

                    string? normalizedPhone = null;
                    if (!string.IsNullOrWhiteSpace(phoneStr))
                    {
                        normalizedPhone = NormalizePhone(phoneStr);
                        if (normalizedPhone.Length is < 10 or > 12 || !normalizedPhone.StartsWith('0'))
                        {
                            response.Errors.Add($"{rowPrefix}: SĐT không hợp lệ.");
                            response.ErrorCount++;
                            continue;
                        }

                        if (existingUserPhones.TryGetValue(normalizedPhone, out var uPhone) && (!isUpdate || uPhone.UserId != updateUserId))
                        {
                            response.Errors.Add($"{rowPrefix}: Số điện thoại đã tồn tại trong hệ thống.");
                            response.ErrorCount++;
                            continue;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(emailStr))
                    {
                        if (existingUserEmails.TryGetValue(emailStr.ToLower(), out var uEmail) && (!isUpdate || uEmail.UserId != updateUserId))
                        {
                            response.Errors.Add($"{rowPrefix}: Email đã tồn tại trong hệ thống.");
                            response.ErrorCount++;
                            continue;
                        }
                    }

                    int? experienceYears = null;
                    if (!string.IsNullOrWhiteSpace(expStr))
                    {
                        if (int.TryParse(expStr, out var exp)) experienceYears = exp;
                        else
                        {
                            response.Errors.Add($"{rowPrefix}: Năm kinh nghiệm không hợp lệ.");
                            response.ErrorCount++;
                            continue;
                        }
                    }

                    User? userToUpdate = null;
                    if (isUpdate)
                    {
                        userToUpdate = await _context.Users.Include(u => u.CenterUsers).FirstOrDefaultAsync(u => u.UserId == updateUserId, cancellationToken);
                        if (userToUpdate == null)
                        {
                            response.Errors.Add($"{rowPrefix}: Không tìm thấy User có ID {updateUserId}.");
                            response.ErrorCount++;
                            continue;
                        }
                        
                        if (!string.IsNullOrWhiteSpace(fullName)) userToUpdate.FullName = fullName;
                        if (!string.IsNullOrWhiteSpace(phoneStr)) userToUpdate.PhoneNumber = phoneStr;
                        if (!string.IsNullOrWhiteSpace(normalizedPhone)) userToUpdate.NormalizedPhoneNumber = normalizedPhone;
                        if (!string.IsNullOrWhiteSpace(identityStr)) userToUpdate.IdentityNumber = identityStr;
                        if (!string.IsNullOrWhiteSpace(emailStr)) userToUpdate.Email = emailStr.ToLower();
                        if (dateOfBirth != null) userToUpdate.DateOfBirth = dateOfBirth.Value;
                        if (!string.IsNullOrWhiteSpace(gender)) userToUpdate.Gender = gender;

                        var membership = userToUpdate.CenterUsers.FirstOrDefault(cu => cu.CenterId == centerId);
                        if (membership == null)
                        {
                            if (string.IsNullOrWhiteSpace(staffCode))
                            {
                                response.Errors.Add($"{rowPrefix}: Cần có Mã nhân sự khi gán nhân sự mới vào trung tâm.");
                                response.ErrorCount++;
                                continue;
                            }
                            membership = new CenterUser
                            {
                                CenterId = centerId.Value,
                                UserId = userToUpdate.UserId,
                                UserType = "TEACHER",
                                StaffCode = staffCode,
                                Status = "Active",
                                CreatedAt = now
                            };
                            _context.CenterUsers.Add(membership);
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(staffCode)) membership.StaffCode = staffCode;
                            if (membership.UserType != "OWNER") membership.UserType = "TEACHER";
                        }
                    }
                    else if (normalizedPhone != null && existingUserPhones.TryGetValue(normalizedPhone, out var existingByPhone))
                    {
                        userToUpdate = existingByPhone;
                    }
                    else if (!string.IsNullOrWhiteSpace(emailStr) && existingUserEmails.TryGetValue(emailStr.ToLower(), out var existingByEmail))
                    {
                        userToUpdate = existingByEmail;
                    }

                    if (userToUpdate != null && !isUpdate) // Found by Phone or Email but not an explicit ID update
                    {
                        if (!string.IsNullOrWhiteSpace(fullName)) userToUpdate.FullName = fullName;
                        if (dateOfBirth != null) userToUpdate.DateOfBirth = dateOfBirth.Value;
                        if (!string.IsNullOrWhiteSpace(gender)) userToUpdate.Gender = gender;
                        
                        var membership = userToUpdate.CenterUsers.FirstOrDefault(cu => cu.CenterId == centerId);
                        if (membership == null)
                        {
                            membership = new CenterUser
                            {
                                CenterId = centerId.Value,
                                UserId = userToUpdate.UserId,
                                UserType = "TEACHER",
                                StaffCode = staffCode,
                                Status = "Active",
                                CreatedAt = now
                            };
                            _context.CenterUsers.Add(membership);
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(staffCode)) membership.StaffCode = staffCode;
                            if (membership.UserType != "OWNER") membership.UserType = "TEACHER";
                        }
                    }
                    else if (userToUpdate == null) // Completely new
                    {
                        var newUser = new User
                        {
                            RoleId = teacherRoleId.Value,
                            FullName = fullName,
                            PhoneNumber = phoneStr,
                            NormalizedPhoneNumber = normalizedPhone,
                            IdentityNumber = string.IsNullOrWhiteSpace(identityStr) ? null : identityStr,
                            Email = string.IsNullOrWhiteSpace(emailStr) ? null : emailStr.ToLower(),
                            DateOfBirth = dateOfBirth ?? default,
                            Gender = gender ?? "Nam",
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("edubridge2026"),
                            EmailConfirmed = true,
                            Status = "Active",
                            IsDeleted = false,
                            CreatedAt = now
                        };
                        
                        _context.Users.Add(newUser);
                        newUser.CenterUsers.Add(new CenterUser
                        {
                            CenterId = centerId.Value,
                            UserType = "TEACHER",
                            StaffCode = staffCode,
                            Status = "Active",
                            CreatedAt = now
                        });
                        userToUpdate = newUser;
                    }

                    var teacherRecord = await _context.Teachers.FirstOrDefaultAsync(t => t.CenterId == centerId && t.UserId == userToUpdate.UserId, cancellationToken);
                    if (teacherRecord == null)
                    {
                        teacherRecord = new Teacher
                        {
                            CenterId = centerId.Value,
                            User = userToUpdate,
                            TeacherCode = string.IsNullOrWhiteSpace(staffCode) ? "TEACHER" : staffCode,
                            Specialization = string.IsNullOrWhiteSpace(specStr) ? null : specStr,
                            ExperienceYears = experienceYears ?? 0,
                            Status = "Active",
                            IsDeleted = false
                        };
                        _context.Teachers.Add(teacherRecord);
                    }
                    else
                    {
                        teacherRecord.TeacherCode = staffCode;
                        if (!string.IsNullOrWhiteSpace(specStr)) teacherRecord.Specialization = specStr;
                        if (experienceYears.HasValue) teacherRecord.ExperienceYears = experienceYears.Value;
                    }

                    response.SuccessCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing row {RowIndex}", rowIndex);
                    response.Errors.Add($"Dòng {rowIndex}: Lỗi hệ thống khi xử lý dữ liệu.");
                    response.ErrorCount++;
                }
                finally
                {
                    if (!isEmptyRow)
                    {
                        actualLastRow = Math.Max(actualLastRow, rowIndex);
                        if (response.ErrorCount > initialErrorCount)
                        {
                            row.Cell(12).Value = "Thất bại";
                            var newErrors = response.Errors.Skip(response.Errors.Count - (response.ErrorCount - initialErrorCount)).ToList();
                            row.Cell(13).Value = string.Join(" | ", newErrors.Select(e => e.Contains(':') ? e.Substring(e.IndexOf(':') + 1).Trim() : e));
                        }
                        else
                        {
                            row.Cell(12).Value = "Thành công";
                            row.Cell(13).Value = "";
                        }
                    }
                }
            }

            if (response.SuccessCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (actualLastRow > 0)
            {
                int maxRow = worksheet.LastRowUsed()?.RowNumber() ?? actualLastRow;
                if (maxRow > actualLastRow)
                {
                    worksheet.Rows(actualLastRow + 1, maxRow).Delete();
                }

                worksheet.Cell(1, 12).Value = "Trạng thái";
                worksheet.Cell(1, 13).Value = "Lý do lỗi";
                
                var headerStyle = worksheet.Cell(1, 11).Style;
                worksheet.Cell(1, 12).Style = headerStyle;
                worksheet.Cell(1, 13).Style = headerStyle;

                var resultRange = worksheet.Range(1, 1, actualLastRow, 13);
                resultRange.Style.Font.FontName = "Times New Roman";
                resultRange.Style.Font.FontSize = 12;
                resultRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                resultRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                worksheet.Columns(1, 13).AdjustToContents();
                for (int i = 1; i <= 13; i++)
                {
                    worksheet.Column(i).Width += 2;
                }

                using var resultStream = new System.IO.MemoryStream();
                workbook.SaveAs(resultStream);
                resultStream.Position = 0;
                var resultFileName = $"result_staff_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                
                var errorFile = new Microsoft.AspNetCore.Http.FormFile(resultStream, 0, resultStream.Length, "result", resultFileName)
                {
                    Headers = new Microsoft.AspNetCore.Http.HeaderDictionary(),
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                };
                
                resultFileUrl = await _storageService.SaveFileAsync(errorFile, "imports/results", cancellationToken);
            }

            await _historyService.UpdateHistoryStatusAsync(history.HistoryId, "Completed", resultFileUrl, cancellationToken);
            
            return ClassOperationResult<EduBridge.Contracts.Students.ImportResultResponse>.Success(response, "Import hoàn tất.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import staffs");
            await _historyService.UpdateHistoryStatusAsync(history.HistoryId, "Error", null, cancellationToken);
            return ClassOperationResult<EduBridge.Contracts.Students.ImportResultResponse>.Failure($"Lỗi khi đọc file Excel: {ex.Message}");
        }
    }

    public async Task<ClassOperationResult<byte[]>> ExportStaffsAsync(int ownerUserId, StaffQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var centerId = await _currentCenterService.GetCenterIdAsync(cancellationToken);
            if (centerId == null)
            {
                var ownerCenter = await _context.Centers.FirstOrDefaultAsync(c => c.OwnerUserId == ownerUserId && c.Status == "Active", cancellationToken);
                if (ownerCenter == null) return ClassOperationResult<byte[]>.Failure("Owner has no center.");
                centerId = ownerCenter.CenterId;
            }

            var queryable = _context.Users.AsNoTracking()
                .Include(u => u.Role)
                .Include(u => u.CenterUsers)
                .Include(u => u.Teacher)
                .Where(u => !u.IsDeleted && u.CenterUsers.Any(cu => cu.CenterId == centerId.Value && (cu.UserType == "TEACHER" || cu.UserType == "OWNER")));

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var keyword = query.Keyword.Trim().ToLower();
                var phone = NormalizePhone(query.Keyword);
                var isPhoneSearch = !string.IsNullOrEmpty(phone) && !query.Keyword.Any(char.IsLetter);
                queryable = queryable.Where(u => 
                    (u.CenterUsers.Any(cu => cu.CenterId == centerId && cu.StaffCode != null && cu.StaffCode.ToLower().Contains(keyword))) ||
                    (u.Teacher != null && u.Teacher.CenterId == centerId && u.Teacher.TeacherCode.ToLower().Contains(keyword)) || 
                    u.FullName.ToLower().Contains(keyword) || 
                    (u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(keyword)) ||
                    (isPhoneSearch && u.NormalizedPhoneNumber != null && u.NormalizedPhoneNumber.Contains(phone)));
            }

            if (!string.IsNullOrWhiteSpace(query.ContactKeyword))
            {
                var contact = query.ContactKeyword.Trim().ToLower();
                queryable = queryable.Where(u => 
                    (u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(contact)) || 
                    (u.Email != null && u.Email.ToLower().Contains(contact)));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
                queryable = queryable.Where(u => u.CenterUsers.Any(cu => cu.CenterId == centerId && cu.Status.ToLower() == query.Status.ToLower()));
                
            if (!string.IsNullOrWhiteSpace(query.Role))
                queryable = queryable.Where(u => u.CenterUsers.Any(cu => cu.CenterId == centerId && cu.UserType.ToLower() == query.Role.ToLower()));

            queryable = queryable.OrderByDescending(u => u.UserId);

            if (query.Page > 0 && query.PageSize > 0)
            {
                queryable = queryable.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize);
            }

            var items = await queryable
                .Select(u => new 
                {
                    User = u,
                    CenterUsers = u.CenterUsers.Where(cu => cu.CenterId == centerId && (cu.UserType == "TEACHER" || cu.UserType == "OWNER")).ToList(),
                    Teacher = u.Teacher != null && u.Teacher.CenterId == centerId && !u.Teacher.IsDeleted ? u.Teacher : null
                })
                .ToListAsync(cancellationToken);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Danh sách nhân sự");

            var headers = new string[] { "STT", "ID", "Mã nhân sự", "Họ và tên", "Ngày sinh", "Giới tính", "CMND/CCCD", "SĐT", "Email", "Vai trò", "Chuyên môn", "Năm kinh nghiệm", "Trạng thái" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var user = item.User;
                var teacher = item.Teacher;
                
                var primaryCu = item.CenterUsers.FirstOrDefault(c => c.UserType == "OWNER") ?? item.CenterUsers.FirstOrDefault();
                var staffCode = primaryCu?.StaffCode;
                if (string.IsNullOrEmpty(staffCode) && teacher != null) staffCode = teacher.TeacherCode;
                
                var roles = item.CenterUsers.Select(c => c.UserType == "TEACHER" ? "Giáo viên" : (c.UserType == "OWNER" ? "Chủ trung tâm" : c.UserType)).ToList();
                var roleString = string.Join(", ", roles);
                var status = primaryCu?.Status == "Active" ? "Đang làm việc" : "Đã nghỉ việc";

                int row = i + 2;
                worksheet.Cell(row, 1).Value = i + 1;
                worksheet.Cell(row, 2).Value = user.UserId;
                worksheet.Cell(row, 3).Value = staffCode ?? "";
                worksheet.Cell(row, 4).Value = user.FullName;
                worksheet.Cell(row, 5).Value = user.DateOfBirth?.ToString("dd/MM/yyyy") ?? "";
                worksheet.Cell(row, 6).Value = user.Gender ?? "";
                worksheet.Cell(row, 7).Value = user.IdentityNumber ?? "";
                worksheet.Cell(row, 8).Value = user.PhoneNumber ?? "";
                worksheet.Cell(row, 9).Value = user.Email ?? "";
                worksheet.Cell(row, 10).Value = roleString;
                worksheet.Cell(row, 11).Value = teacher?.Specialization ?? "";
                worksheet.Cell(row, 12).Value = teacher != null ? teacher.ExperienceYears.ToString() : "";
                worksheet.Cell(row, 13).Value = status;
                
                for (int col = 1; col <= 13; col++)
                {
                    worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
            }

            worksheet.Style.Font.FontName = "Times New Roman";
            worksheet.Style.Font.FontSize = 12;

            worksheet.Columns().AdjustToContents();
            for (int i = 1; i <= 13; i++)
            {
                worksheet.Column(i).Width += 2;
            }

            worksheet.Column(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Column(2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Column(5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Column(12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            // Save history
            var history = await _historyService.CreateHistoryAsync(new EduBridge.Contracts.ImportExportHistories.CreateImportExportHistoryRequest
            {
                CenterId = centerId.Value,
                UserId = ownerUserId,
                Title = "Xuất danh sách nhân sự",
                ActionType = "Export",
                EntityName = "Staffs"
            }, cancellationToken);
            
            await _historyService.UpdateHistoryStatusAsync(history.HistoryId, "Completed", null, cancellationToken);

            return ClassOperationResult<byte[]>.Success(content, "Xuất dữ liệu thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export staffs");
            return ClassOperationResult<byte[]>.Failure("Lỗi khi xuất file Excel.");
        }
    }
}
