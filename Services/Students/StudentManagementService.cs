using System.Data;
using System.Text.RegularExpressions;
using EduBridge.Contracts.Students;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Services.Classes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EduBridge.Services.Auth;
using ClosedXML.Excel;

namespace EduBridge.Services.Students;

public class StudentManagementService : IStudentManagementService
{
    private readonly AppDbContext _context;
    private readonly ILogger<StudentManagementService> _logger;
    private readonly ICurrentCenterService _currentCenterService;
    private readonly EduBridge.Services.ImportExportHistories.IImportExportHistoryService _historyService;
    private readonly EduBridge.Services.Storage.IFileStorageService _storageService;

    public StudentManagementService(
        AppDbContext context, 
        ILogger<StudentManagementService> logger, 
        ICurrentCenterService currentCenterService,
        EduBridge.Services.ImportExportHistories.IImportExportHistoryService historyService,
        EduBridge.Services.Storage.IFileStorageService storageService)
    {
        _context = context;
        _logger = logger;
        _currentCenterService = currentCenterService;
        _historyService = historyService;
        _storageService = storageService;
    }

    private async Task<int?> GetOwnerCenterIdAsync(int ownerUserId, CancellationToken cancellationToken) =>
        await _currentCenterService.GetCenterIdAsync(cancellationToken);

    private static string NormalizePhoneNumber(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.StartsWith("84", StringComparison.Ordinal) && digits.Length > 9
            ? $"0{digits[2..]}"
            : digits;
    }

    private static string? NormalizeOptionalPhoneNumber(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : NormalizePhoneNumber(value);
    }

    private static DateOnly GetVietnamToday()
    {
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
        }
        catch (TimeZoneNotFoundException)
        {
            return DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        }
    }

    private static bool IsValidVietnamPhone(string? normalizedPhone)
    {
        return !string.IsNullOrWhiteSpace(normalizedPhone) &&
               normalizedPhone.Length is >= 10 and <= 12 &&
               normalizedPhone.StartsWith('0') &&
               normalizedPhone.All(char.IsDigit);
    }

    private static bool IsValidIdentityNumber(string value)
    {
        return value.All(char.IsDigit) && value.Length is 9 or 12;
    }

    public async Task<ClassOperationResult<StudentPagedResponse>> GetStudentsAsync(int ownerUserId, StudentQuery query, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return ClassOperationResult<StudentPagedResponse>.Failure("Không tìm thấy trung tâm hoặc bạn không có quyền.");

        query.Normalize();

        var dbQuery = _context.Students
            .AsNoTracking()
            .Where(s => s.CenterId == centerId.Value && !s.IsDeleted && s.ParentUser != null && !s.ParentUser.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            dbQuery = dbQuery.Where(s => s.StudentCode.Contains(query.Keyword) || s.FullName.Contains(query.Keyword));
        }

        if (!string.IsNullOrWhiteSpace(query.ParentKeyword))
        {
            dbQuery = dbQuery.Where(s => s.ParentUser != null && s.ParentUser.FullName.Contains(query.ParentKeyword));
        }

        if (!string.IsNullOrWhiteSpace(query.ContactKeyword))
        {
            var normalizedKeyword = NormalizePhoneNumber(query.ContactKeyword);
            var hasPhoneKeyword = !string.IsNullOrWhiteSpace(normalizedKeyword);
            dbQuery = dbQuery.Where(s =>
                (s.PhoneNumber != null && s.PhoneNumber.Contains(query.ContactKeyword)) ||
                (s.Email != null && s.Email.Contains(query.ContactKeyword)) ||
                (hasPhoneKeyword && s.NormalizedPhoneNumber != null && s.NormalizedPhoneNumber.Contains(normalizedKeyword)) ||
                (s.ParentUser != null && s.ParentUser.PhoneNumber != null && s.ParentUser.PhoneNumber.Contains(query.ContactKeyword)) ||
                (hasPhoneKeyword && s.ParentUser != null && s.ParentUser.NormalizedPhoneNumber != null && s.ParentUser.NormalizedPhoneNumber.Contains(normalizedKeyword)) ||
                (s.ParentUser != null && s.ParentUser.Email != null && s.ParentUser.Email.Contains(query.ContactKeyword)));
        }

        if (!string.IsNullOrWhiteSpace(query.Gender))
        {
            dbQuery = dbQuery.Where(s => s.Gender == query.Gender);
        }

        if (query.ClassId != null)
        {
            dbQuery = dbQuery.Where(s => s.Enrollments.Any(e => e.ClassId == query.ClassId.Value && e.Status == "Đang học"));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            dbQuery = dbQuery.Where(s => s.Status == query.Status);
        }

        var totalItems = await dbQuery.CountAsync(cancellationToken);

        var students = await dbQuery
            .OrderByDescending(s => s.StudentId)
            .Select(s => new StudentResponse
            {
                StudentId = s.StudentId,
                StudentCode = s.StudentCode,
                FullName = s.FullName,
                Gender = s.Gender,
                PhoneNumber = s.PhoneNumber,
                Email = s.Email,
                DateOfBirth = s.DateOfBirth,
                Status = s.Status,
                AvatarUrl = s.AvatarUrl,
                Ethnicity = s.Ethnicity,
                Religion = s.Religion,
                IdentityNumber = s.IdentityNumber,
                IdentityIssuedDate = s.IdentityIssuedDate,
                IdentityIssuedPlace = s.IdentityIssuedPlace,
                CurrentAddress = s.Address,
                PermanentAddress = s.PermanentAddress,
                Hometown = s.Hometown,
                PlaceOfBirth = s.PlaceOfBirth,
                ParentUserId = s.ParentUserId ?? 0,
                ParentName = s.ParentUser != null ? s.ParentUser.FullName : string.Empty,
                ParentPhone = s.ParentUser != null ? s.ParentUser.PhoneNumber : string.Empty,
                ParentEmail = s.ParentUser != null ? s.ParentUser.Email : string.Empty,
                CurrentClasses = s.Enrollments
                    .Where(e => e.Status == "Đang học")
                    .OrderByDescending(e => e.EnrollmentId)
                    .Select(e => new StudentClassResponse
                    {
                        ClassId = e.ClassId,
                        ClassCode = e.Class.ClassCode,
                        ClassName = e.Class.ClassName,
                        CourseName = e.Class.Course.CourseName
                    })
                    .ToList()
            })
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return ClassOperationResult<StudentPagedResponse>.Success(new StudentPagedResponse
        {
            Data = students,
            TotalItems = totalItems,
            Page = query.PageNumber,
            PageSize = query.PageSize
        }, "Success");
    }

    public async Task<ClassOperationResult<StudentResponse>> GetStudentAsync(int ownerUserId, int studentId, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return ClassOperationResult<StudentResponse>.Failure("Không tìm thấy trung tâm hoặc bạn không có quyền.");

        var student = await _context.Students
            .AsNoTracking()
            .Where(s => s.CenterId == centerId.Value && s.StudentId == studentId && !s.IsDeleted)
            .Select(s => new StudentResponse
            {
                StudentId = s.StudentId,
                StudentCode = s.StudentCode,
                FullName = s.FullName,
                Gender = s.Gender,
                PhoneNumber = s.PhoneNumber,
                Email = s.Email,
                DateOfBirth = s.DateOfBirth,
                Status = s.Status,
                AvatarUrl = s.AvatarUrl,
                Ethnicity = s.Ethnicity,
                Religion = s.Religion,
                IdentityNumber = s.IdentityNumber,
                IdentityIssuedDate = s.IdentityIssuedDate,
                IdentityIssuedPlace = s.IdentityIssuedPlace,
                CurrentAddress = s.Address,
                PermanentAddress = s.PermanentAddress,
                Hometown = s.Hometown,
                PlaceOfBirth = s.PlaceOfBirth,
                ParentUserId = s.ParentUserId ?? 0,
                ParentName = s.ParentUser != null ? s.ParentUser.FullName : string.Empty,
                ParentPhone = s.ParentUser != null ? s.ParentUser.PhoneNumber : string.Empty,
                ParentEmail = s.ParentUser != null ? s.ParentUser.Email : string.Empty,
                CurrentClasses = s.Enrollments
                    .Where(e => e.Status == "Đang học")
                    .OrderByDescending(e => e.EnrollmentId)
                    .Select(e => new StudentClassResponse
                    {
                        ClassId = e.ClassId,
                        ClassCode = e.Class.ClassCode,
                        ClassName = e.Class.ClassName,
                        CourseName = e.Class.Course.CourseName
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (student == null) return ClassOperationResult<StudentResponse>.Failure("Không tìm thấy học sinh.");

        return ClassOperationResult<StudentResponse>.Success(student, "Success");
    }

    public async Task<ClassOperationResult<List<ParentSearchResultResponse>>> SearchParentsAsync(int ownerUserId, string keyword, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return ClassOperationResult<List<ParentSearchResultResponse>>.Failure("Không tìm thấy trung tâm.");

        var normalizedKeyword = string.IsNullOrWhiteSpace(keyword) ? null : Regex.Replace(keyword.Trim(), @"\s+", " ");
        if (string.IsNullOrWhiteSpace(normalizedKeyword) || normalizedKeyword.Length < 3)
        {
            return ClassOperationResult<List<ParentSearchResultResponse>>.Failure("Vui lòng nhập ít nhất 3 ký tự.");
        }

        var normalizedPhone = NormalizeOptionalPhoneNumber(normalizedKeyword);
        var hasPhoneKeyword = !string.IsNullOrWhiteSpace(normalizedPhone);

        var parents = await _context.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted && u.Role.RoleCode == "PARENT")
            .Where(u => _context.CenterUsers.Any(cu =>
                cu.CenterId == centerId.Value &&
                cu.UserId == u.UserId &&
                cu.UserType == "PARENT" &&
                cu.Status == "Active"))
            .Where(u =>
                (u.Email != null && u.Email.Contains(normalizedKeyword)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(normalizedKeyword)) ||
                (hasPhoneKeyword && u.NormalizedPhoneNumber != null && u.NormalizedPhoneNumber.Contains(normalizedPhone!)))
            .OrderBy(u => u.FullName)
            .Take(10)
            .Select(u => new ParentSearchResultResponse
            {
                UserId = u.UserId,
                FullName = u.FullName,
                PhoneNumber = u.PhoneNumber,
                Email = u.Email,
                Status = u.Status
            })
            .ToListAsync(cancellationToken);

        return ClassOperationResult<List<ParentSearchResultResponse>>.Success(parents, "Success");
    }

    public async Task<ClassOperationResult<StudentMutationResponse>> CreateStudentAsync(int ownerUserId, SaveStudentRequest request, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return ClassOperationResult<StudentMutationResponse>.Failure("Không tìm thấy trung tâm.");

        request.Normalize();

        var normalizedStudentPhone = NormalizeOptionalPhoneNumber(request.StudentPhoneNumber);
        var normalizedParentPhone = NormalizeOptionalPhoneNumber(request.ParentPhoneNumber);

        // Validation
        if (!Regex.IsMatch(request.StudentCode, @"^[A-Za-z0-9][A-Za-z0-9\-_]{1,29}$"))
            return ClassOperationResult<StudentMutationResponse>.Failure("Mã học sinh chỉ gồm chữ, số, dấu gạch ngang hoặc gạch dưới, dài 2-30 ký tự.");
        
        if (request.DateOfBirth > GetVietnamToday())
            return ClassOperationResult<StudentMutationResponse>.Failure("Ngày sinh không được lớn hơn ngày hiện tại.");

        if (request.IdentityIssuedDate != null && request.IdentityIssuedDate > GetVietnamToday())
            return ClassOperationResult<StudentMutationResponse>.Failure("Ngày cấp CMND/CCCD không được lớn hơn ngày hiện tại.");

        if (!string.IsNullOrWhiteSpace(request.IdentityNumber))
        {
            if (!IsValidIdentityNumber(request.IdentityNumber))
                return ClassOperationResult<StudentMutationResponse>.Failure("CMND/CCCD phải gồm 9 hoặc 12 số.");

            var identityExists = await _context.Students.AnyAsync(s => s.CenterId == centerId && !s.IsDeleted && s.IdentityNumber == request.IdentityNumber, cancellationToken);
            if (identityExists) return ClassOperationResult<StudentMutationResponse>.Failure("CMND/CCCD học sinh đã tồn tại.");
        }

        if (!string.IsNullOrWhiteSpace(request.StudentPhoneNumber) && !IsValidVietnamPhone(normalizedStudentPhone))
            return ClassOperationResult<StudentMutationResponse>.Failure("Số điện thoại học sinh không hợp lệ.");

        if (request.ParentUserId == null && !IsValidVietnamPhone(normalizedParentPhone))
            return ClassOperationResult<StudentMutationResponse>.Failure("Số điện thoại phụ huynh không hợp lệ.");

        var studentCodeExists = await _context.Students.AnyAsync(s => s.CenterId == centerId && !s.IsDeleted && s.StudentCode == request.StudentCode, cancellationToken);
        if (studentCodeExists) return ClassOperationResult<StudentMutationResponse>.Failure("Mã học sinh đã tồn tại.");

        if (!string.IsNullOrWhiteSpace(normalizedStudentPhone))
        {
            var studentPhoneExists = await _context.Students.AnyAsync(s => s.CenterId == centerId && !s.IsDeleted && s.NormalizedPhoneNumber == normalizedStudentPhone, cancellationToken);
            if (studentPhoneExists) return ClassOperationResult<StudentMutationResponse>.Failure("Số điện thoại học sinh đã tồn tại.");
        }

        if (!string.IsNullOrWhiteSpace(request.StudentEmail))
        {
            var studentEmailExists = await _context.Students.AnyAsync(s => s.CenterId == centerId && !s.IsDeleted && s.Email != null && s.Email == request.StudentEmail, cancellationToken);
            if (studentEmailExists) return ClassOperationResult<StudentMutationResponse>.Failure("Email học sinh đã tồn tại.");
        }

        // Parent resolution
        var parentRoleId = await _context.Roles.Where(r => r.RoleCode == "PARENT").Select(r => (int?)r.RoleId).FirstOrDefaultAsync(cancellationToken);
        if (parentRoleId == null) return ClassOperationResult<StudentMutationResponse>.Failure("Hệ thống chưa cấu hình role PARENT.");

        User? parent = null;
        if (request.ParentUserId != null)
        {
            parent = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == request.ParentUserId.Value && !u.IsDeleted && _context.CenterUsers.Any(cu => cu.CenterId == centerId && cu.UserId == u.UserId && cu.UserType == "PARENT" && cu.Status == "Active"), cancellationToken);
            if (parent == null || !parent.Role.RoleCode.Equals("PARENT", StringComparison.OrdinalIgnoreCase))
                return ClassOperationResult<StudentMutationResponse>.Failure("Tài khoản phụ huynh đã chọn không hợp lệ hoặc không thuộc trung tâm.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(normalizedParentPhone)) return ClassOperationResult<StudentMutationResponse>.Failure("Phải có SĐT phụ huynh nếu tạo mới.");
            
            parent = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => !u.IsDeleted && u.NormalizedPhoneNumber == normalizedParentPhone && _context.CenterUsers.Any(cu => cu.CenterId == centerId && cu.UserId == u.UserId && cu.UserType == "PARENT" && cu.Status == "Active"), cancellationToken);
            User? parentByEmail = null;
            if (!string.IsNullOrWhiteSpace(request.ParentEmail))
            {
                parentByEmail = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => !u.IsDeleted && u.Email == request.ParentEmail && _context.CenterUsers.Any(cu => cu.CenterId == centerId && cu.UserId == u.UserId && cu.UserType == "PARENT" && cu.Status == "Active"), cancellationToken);
            }

            if (parent != null && parentByEmail != null && parent.UserId != parentByEmail.UserId)
                return ClassOperationResult<StudentMutationResponse>.Failure("SĐT và email phụ huynh đang thuộc 2 tài khoản khác nhau.");
            
            parent ??= parentByEmail;
            if (parent != null && !parent.Role.RoleCode.Equals("PARENT", StringComparison.OrdinalIgnoreCase))
                return ClassOperationResult<StudentMutationResponse>.Failure("Tài khoản này không phải phụ huynh.");
        }

        string? uploadedAvatarUrl = await SaveAvatarAsync(request.AvatarFile, request.StudentCode, cancellationToken);
        var now = DateTime.UtcNow;

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            if (parent == null)
            {
                parent = new User
                {
                    RoleId = parentRoleId.Value,
                    FullName = request.ParentFullName ?? "Phụ huynh " + request.FullName,
                    Email = request.ParentEmail,
                    PhoneNumber = request.ParentPhoneNumber,
                    NormalizedPhoneNumber = normalizedParentPhone,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("edubridge2026"),
                    EmailConfirmed = true,
                    Status = "Active",
                    IsDeleted = false,
                    CreatedAt = now
                };
                _context.Users.Add(parent);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var centerUser = await _context.CenterUsers.FirstOrDefaultAsync(cu => cu.CenterId == centerId && cu.UserId == parent.UserId && cu.UserType == "PARENT", cancellationToken);
            if (centerUser == null)
            {
                _context.CenterUsers.Add(new CenterUser { CenterId = centerId.Value, UserId = parent.UserId, UserType = "PARENT", Status = "Active", CreatedAt = now });
            }
            else
            {
                centerUser.Status = "Active";
            }
            await _context.SaveChangesAsync(cancellationToken);

            var student = new Student
            {
                ParentUserId = parent.UserId,
                CenterId = centerId.Value,
                StudentCode = request.StudentCode,
                FullName = request.FullName,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Address = request.CurrentAddress,
                PhoneNumber = request.StudentPhoneNumber,
                Email = request.StudentEmail,
                NormalizedPhoneNumber = normalizedStudentPhone,
                AvatarUrl = uploadedAvatarUrl,
                Ethnicity = request.Ethnicity,
                Religion = request.Religion,
                IdentityNumber = request.IdentityNumber,
                IdentityIssuedDate = request.IdentityIssuedDate,
                IdentityIssuedPlace = request.IdentityIssuedPlace,
                PermanentAddress = request.PermanentAddress,
                Hometown = request.Hometown,
                PlaceOfBirth = request.PlaceOfBirth,
                Status = "Active",
                IsDeleted = false,
                CreatedAt = now
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ClassOperationResult<StudentMutationResponse>.Success(new StudentMutationResponse(student.StudentId, student.StudentCode, student.Status), "Success");
        }
        catch (DbUpdateException)
        {
            DeleteUploadedAvatar(uploadedAvatarUrl);
            return ClassOperationResult<StudentMutationResponse>.Failure("Lỗi cập nhật CSDL. Vui lòng thử lại.");
        }
    }

    public async Task<ClassOperationResult<StudentMutationResponse>> UpdateStudentAsync(int ownerUserId, int studentId, UpdateStudentRequest request, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return ClassOperationResult<StudentMutationResponse>.Failure("Không tìm thấy trung tâm.");

        var student = await _context.Students.FirstOrDefaultAsync(s => s.CenterId == centerId && s.StudentId == studentId && !s.IsDeleted, cancellationToken);
        if (student == null) return ClassOperationResult<StudentMutationResponse>.Failure("Không tìm thấy học sinh.");

        request.Normalize();
        var normalizedStudentPhone = NormalizeOptionalPhoneNumber(request.StudentPhoneNumber);

        // Validation
        if (request.DateOfBirth > GetVietnamToday())
            return ClassOperationResult<StudentMutationResponse>.Failure("Ngày sinh không hợp lệ.");

        if (!string.IsNullOrWhiteSpace(request.IdentityNumber) && !IsValidIdentityNumber(request.IdentityNumber))
            return ClassOperationResult<StudentMutationResponse>.Failure("CMND/CCCD phải có 9 hoặc 12 số.");

        if (!string.IsNullOrWhiteSpace(request.IdentityNumber) && student.IdentityNumber != request.IdentityNumber)
        {
            var exists = await _context.Students.AnyAsync(s => s.CenterId == centerId && !s.IsDeleted && s.IdentityNumber == request.IdentityNumber && s.StudentId != studentId, cancellationToken);
            if (exists) return ClassOperationResult<StudentMutationResponse>.Failure("CMND/CCCD học sinh đã tồn tại.");
        }

        if (!string.IsNullOrWhiteSpace(request.StudentPhoneNumber) && student.NormalizedPhoneNumber != normalizedStudentPhone)
        {
            if (!IsValidVietnamPhone(normalizedStudentPhone)) return ClassOperationResult<StudentMutationResponse>.Failure("SĐT không hợp lệ.");
            var exists = await _context.Students.AnyAsync(s => s.CenterId == centerId && !s.IsDeleted && s.NormalizedPhoneNumber == normalizedStudentPhone && s.StudentId != studentId, cancellationToken);
            if (exists) return ClassOperationResult<StudentMutationResponse>.Failure("SĐT đã tồn tại.");
        }

        if (!string.IsNullOrWhiteSpace(request.StudentEmail) && student.Email != request.StudentEmail)
        {
            var exists = await _context.Students.AnyAsync(s => s.CenterId == centerId && !s.IsDeleted && s.Email == request.StudentEmail && s.StudentId != studentId, cancellationToken);
            if (exists) return ClassOperationResult<StudentMutationResponse>.Failure("Email đã tồn tại.");
        }

        var oldAvatar = student.AvatarUrl;
        string? newAvatar = null;
        if (request.AvatarFile != null)
        {
            newAvatar = await SaveAvatarAsync(request.AvatarFile, student.StudentCode, cancellationToken);
            student.AvatarUrl = newAvatar;
        }
        else if (request.RemoveAvatar)
        {
            student.AvatarUrl = null;
        }

        student.FullName = request.FullName;
        student.DateOfBirth = request.DateOfBirth;
        student.Gender = request.Gender;
        student.Ethnicity = request.Ethnicity;
        student.Religion = request.Religion;
        student.IdentityNumber = request.IdentityNumber;
        student.IdentityIssuedDate = request.IdentityIssuedDate;
        student.IdentityIssuedPlace = request.IdentityIssuedPlace;
        student.Address = request.CurrentAddress;
        student.PermanentAddress = request.PermanentAddress;
        student.Hometown = request.Hometown;
        student.PlaceOfBirth = request.PlaceOfBirth;
        student.PhoneNumber = request.StudentPhoneNumber;
        student.Email = request.StudentEmail;
        student.NormalizedPhoneNumber = normalizedStudentPhone;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            if (request.RemoveAvatar || (request.AvatarFile != null && oldAvatar != null))
            {
                DeleteUploadedAvatar(oldAvatar);
            }
            return ClassOperationResult<StudentMutationResponse>.Success(new StudentMutationResponse(student.StudentId, student.StudentCode, student.Status), "Success");
        }
        catch (DbUpdateException)
        {
            if (newAvatar != null) DeleteUploadedAvatar(newAvatar);
            return ClassOperationResult<StudentMutationResponse>.Failure("Lỗi cập nhật CSDL.");
        }
    }

    public async Task<ClassOperationResult<StudentMutationResponse>> UpdateStudentParentAsync(int ownerUserId, int studentId, UpdateStudentParentRequest request, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return ClassOperationResult<StudentMutationResponse>.Failure("Không tìm thấy trung tâm.");

        var student = await _context.Students.FirstOrDefaultAsync(s => s.CenterId == centerId && s.StudentId == studentId && !s.IsDeleted, cancellationToken);
        if (student == null) return ClassOperationResult<StudentMutationResponse>.Failure("Không tìm thấy học sinh.");

        var newParent = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == request.ParentUserId && !u.IsDeleted && _context.CenterUsers.Any(cu => cu.CenterId == centerId && cu.UserId == u.UserId && cu.UserType == "PARENT" && cu.Status == "Active"), cancellationToken);
        if (newParent == null || !newParent.Role.RoleCode.Equals("PARENT", StringComparison.OrdinalIgnoreCase))
        {
            return ClassOperationResult<StudentMutationResponse>.Failure("Tài khoản phụ huynh không hợp lệ hoặc không thuộc trung tâm.");
        }

        student.ParentUserId = newParent.UserId;
        await _context.SaveChangesAsync(cancellationToken);

        return ClassOperationResult<StudentMutationResponse>.Success(new StudentMutationResponse(student.StudentId, student.StudentCode, student.Status), "Success");
    }

    public async Task<ClassOperationResult<StudentMutationResponse>> ToggleStudentStatusAsync(int ownerUserId, int studentId, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return ClassOperationResult<StudentMutationResponse>.Failure("Không tìm thấy trung tâm.");

        var student = await _context.Students.FirstOrDefaultAsync(s => s.CenterId == centerId && s.StudentId == studentId && !s.IsDeleted, cancellationToken);
        if (student == null) return ClassOperationResult<StudentMutationResponse>.Failure("Không tìm thấy học sinh.");

        if (student.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            var hasActiveEnrollment = await _context.Enrollments.AnyAsync(e => e.StudentId == studentId && e.Status == "Đang học", cancellationToken);
            if (hasActiveEnrollment) return ClassOperationResult<StudentMutationResponse>.Failure("Học sinh vẫn còn lớp đang học, không thể ngừng hoạt động.");
            student.Status = "Inactive";
        }
        else
        {
            student.Status = "Active";
        }

        await _context.SaveChangesAsync(cancellationToken);
        return ClassOperationResult<StudentMutationResponse>.Success(new StudentMutationResponse(student.StudentId, student.StudentCode, student.Status), "Success");
    }

    public async Task<ClassOperationResult<bool>> DeleteStudentAsync(int ownerUserId, int studentId, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return ClassOperationResult<bool>.Failure("Không tìm thấy trung tâm.");

        var student = await _context.Students.FirstOrDefaultAsync(s => s.CenterId == centerId && s.StudentId == studentId && !s.IsDeleted, cancellationToken);
        if (student == null) return ClassOperationResult<bool>.Failure("Không tìm thấy học sinh.");

        if (!student.Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
        {
            return ClassOperationResult<bool>.Failure("Chỉ có thể xóa học sinh đã ngừng hoạt động.");
        }

        try
        {
            student.IsDeleted = true;
            student.DeletedAt = DateTime.UtcNow;
            student.DeletedByUserId = ownerUserId;
            student.Status = "Inactive";

            await _context.SaveChangesAsync(cancellationToken);
            return ClassOperationResult<bool>.Success(true, "Success");
        }
        catch (DbUpdateException)
        {
            return ClassOperationResult<bool>.Failure("Không thể xóa học sinh vì dữ liệu đang được liên kết.");
        }
    }

    private static async Task<string?> SaveAvatarAsync(IFormFile? avatarFile, string studentCode, CancellationToken cancellationToken)
    {
        if (avatarFile == null || avatarFile.Length == 0) return null;

        var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "students");
        Directory.CreateDirectory(uploadDirectory);

        var extension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
        var fileName = $"{studentCode.ToLowerInvariant()}-{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadDirectory, fileName);

        await using var stream = System.IO.File.Create(fullPath);
        await avatarFile.CopyToAsync(stream, cancellationToken);

        return $"/uploads/students/{fileName}";
    }

    private static void DeleteUploadedAvatar(string? avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl)) return;

        var relativePath = avatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }

    public async Task<ClassOperationResult<ImportResultResponse>> ImportStudentsFromExcelAsync(int ownerUserId, Microsoft.AspNetCore.Http.IFormFile importFile, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return ClassOperationResult<ImportResultResponse>.Failure("Không tìm thấy trung tâm.");

        var response = new ImportResultResponse();
        var now = DateTime.UtcNow;
        var parentRoleId = await _context.Roles.Where(r => r.RoleCode == "PARENT").Select(r => (int?)r.RoleId).FirstOrDefaultAsync(cancellationToken);
        if (parentRoleId == null) return ClassOperationResult<ImportResultResponse>.Failure("Hệ thống chưa cấu hình role PARENT.");

        var inputFileUrl = await _storageService.SaveFileAsync(importFile, "imports", cancellationToken);

        var history = await _historyService.CreateHistoryAsync(new EduBridge.Contracts.ImportExportHistories.CreateImportExportHistoryRequest
        {
            CenterId = centerId.Value,
            UserId = ownerUserId,
            Title = $"Import học sinh {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
            ActionType = "Import",
            EntityName = "Students",
            InputFileUrl = inputFileUrl,
            Status = "Processing"
        }, cancellationToken);

        string? resultFileUrl = null;
        try
        {
            using var excelStream = importFile.OpenReadStream();
            using var workbook = new XLWorkbook(excelStream);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1).ToList(); // Skip header and materialize

            var phoneNumbersInExcel = new HashSet<string>();
            var emailsInExcel = new HashSet<string>();
            foreach (var row in rows)
            {
                var phone = row.Cell(10).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(phone))
                {
                    // Basic normalization to find existing users (removing spaces/special chars if any, although NormalizeOptionalPhoneNumber handles it)
                    var normPhone = string.Concat(phone.Where(char.IsDigit));
                    if (normPhone.StartsWith("84")) normPhone = "0" + normPhone.Substring(2);
                    if (normPhone.StartsWith("+84")) normPhone = "0" + normPhone.Substring(3);
                    phoneNumbersInExcel.Add(normPhone);
                }

                var email = row.Cell(12).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(email))
                {
                    emailsInExcel.Add(email.ToLower());
                }
            }

            var phoneNumbersList = phoneNumbersInExcel.ToList();
            var existingUsers = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.CenterUsers)
                .Where(u => !u.IsDeleted && u.RoleId == parentRoleId && u.NormalizedPhoneNumber != null && phoneNumbersList.Contains(u.NormalizedPhoneNumber))
                .ToListAsync(cancellationToken);

            var emailsList = emailsInExcel.ToList();
            var existingEmailsSet = new HashSet<string>(await _context.Users
                .Where(u => !u.IsDeleted && u.Email != null && emailsList.Contains(u.Email))
                .Select(u => u.Email!)
                .ToListAsync(cancellationToken), StringComparer.OrdinalIgnoreCase);

            var existingUserPhones = existingUsers
                .Where(u => !string.IsNullOrEmpty(u.NormalizedPhoneNumber))
                .GroupBy(u => u.NormalizedPhoneNumber!)
                .ToDictionary(g => g.Key, g => g.First());
                
            var existingStudents = await _context.Students
                .Where(s => s.CenterId == centerId && !s.IsDeleted)
                .ToListAsync(cancellationToken);
                
            var existingStudentsById = existingStudents
                .GroupBy(s => s.StudentId)
                .ToDictionary(g => g.Key, g => g.First());
            var existingStudentsByCode = existingStudents
                .GroupBy(s => s.StudentCode.ToLower())
                .ToDictionary(g => g.Key, g => g.First());
            var existingStudentsByPhone = new Dictionary<string, Student>();
            foreach(var s in existingStudents)
            {
                if (!string.IsNullOrEmpty(s.NormalizedPhoneNumber) && !existingStudentsByPhone.ContainsKey(s.NormalizedPhoneNumber))
                {
                    existingStudentsByPhone[s.NormalizedPhoneNumber] = s;
                }
            }
            int actualLastRow = 1;
            foreach (var row in rows)
            {
                response.TotalRows++;
                var rowIndex = row.RowNumber();
                int initialErrorCount = response.ErrorCount;
                bool isEmptyRow = false;
                bool isUpdate = false;
                
                try
                {
                    var sttStr = row.Cell(1).GetString().Trim();
                    var idStr = row.Cell(2).GetString().Trim();
                    var studentCode = row.Cell(3).GetString().Trim();
                    var fullName = row.Cell(4).GetString().Trim();
                    var dobStr = row.Cell(5).GetString().Trim();
                    var genderStr = row.Cell(6).GetString().Trim();
                    var studentPhone = row.Cell(7).GetString().Trim();
                    var studentEmail = row.Cell(8).GetString().Trim();
                    var address = row.Cell(9).GetString().Trim();
                    var parentPhone = row.Cell(10).GetString().Trim();
                    var parentFullName = row.Cell(11).GetString().Trim();
                    var parentEmail = row.Cell(12).GetString().Trim();

                    var rowPrefix = string.IsNullOrWhiteSpace(sttStr) ? $"Dòng {rowIndex}" : $"STT {sttStr}";

                    // Bỏ qua các dòng trống hoàn toàn (có thể do các cột ghi chú bên phải làm XLWorkbook nhận diện là row used)
                    if (string.IsNullOrWhiteSpace(sttStr) && string.IsNullOrWhiteSpace(idStr) && string.IsNullOrWhiteSpace(studentCode) && 
                        string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(parentPhone))
                    {
                        isEmptyRow = true;
                        response.TotalRows--; // Không tính là một dòng dữ liệu hợp lệ
                        continue;
                    }
                    int studentId = 0;
                    Student? existingStudentToUpdate = null;
                    if (!string.IsNullOrWhiteSpace(idStr))
                    {
                        if (int.TryParse(idStr, out studentId) && studentId > 0)
                        {
                            if (!existingStudentsById.TryGetValue(studentId, out existingStudentToUpdate))
                            {
                                response.Errors.Add($"{rowPrefix}: Không tìm thấy học sinh với ID {studentId} trong hệ thống.");
                                response.ErrorCount++;
                                continue;
                            }
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
                        if (string.IsNullOrWhiteSpace(studentCode)) missingFields.Add("Mã học sinh");
                        if (string.IsNullOrWhiteSpace(fullName)) missingFields.Add("Họ và tên");
                        if (string.IsNullOrWhiteSpace(genderStr)) missingFields.Add("Giới tính");
                        if (string.IsNullOrWhiteSpace(parentPhone)) missingFields.Add("SĐT phụ huynh");
                        if (string.IsNullOrWhiteSpace(parentFullName)) missingFields.Add("Tên phụ huynh");

                        if (missingFields.Any())
                        {
                            response.Errors.Add($"{rowPrefix}: Thiếu thông tin bắt buộc ở các cột ({string.Join(", ", missingFields)}).");
                            response.ErrorCount++;
                            continue;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(studentCode) && !Regex.IsMatch(studentCode, @"^[A-Za-z0-9][A-Za-z0-9\-_]{1,29}$"))
                    {
                        response.Errors.Add($"{rowPrefix}: Mã HS không hợp lệ (chỉ gồm chữ, số, dấu -, _, 2-30 ký tự).");
                        response.ErrorCount++;
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(studentCode) && existingStudentsByCode.TryGetValue(studentCode.ToLower(), out var sCode) && (!isUpdate || sCode.StudentId != studentId))
                    {
                        response.Errors.Add($"{rowPrefix}: Mã học sinh đã tồn tại.");
                        response.ErrorCount++;
                        continue;
                    }

                    string? gender = null;
                    if (!string.IsNullOrWhiteSpace(genderStr))
                    {
                        if (genderStr == "1") gender = "Nam";
                        else if (genderStr == "2") gender = "Nữ";
                        else
                        {
                            response.Errors.Add($"{rowPrefix}: Giới tính không hợp lệ (1=Nam, 2=Nữ).");
                            response.ErrorCount++;
                            continue;
                        }
                    }

                    DateOnly? dateOfBirth = null;
                    if (!string.IsNullOrWhiteSpace(dobStr))
                    {
                        if (DateOnly.TryParseExact(dobStr, new[] { "dd/MM/yyyy", "d/M/yyyy" }, null, System.Globalization.DateTimeStyles.None, out var dob))
                        {
                            if (dob > GetVietnamToday())
                            {
                                response.Errors.Add($"{rowPrefix}: Ngày sinh không được lớn hơn ngày hiện tại.");
                                response.ErrorCount++;
                                continue;
                            }
                            dateOfBirth = dob;
                        }
                        else
                        {
                            response.Errors.Add($"{rowPrefix}: Ngày sinh không đúng định dạng dd/MM/yyyy.");
                            response.ErrorCount++;
                            continue;
                        }
                    }

                    var normalizedStudentPhone = NormalizeOptionalPhoneNumber(studentPhone);
                    if (!string.IsNullOrWhiteSpace(normalizedStudentPhone))
                    {
                        if (!IsValidVietnamPhone(normalizedStudentPhone))
                        {
                            response.Errors.Add($"{rowPrefix}: SĐT học sinh không hợp lệ.");
                            response.ErrorCount++;
                            continue;
                        }
                        if (existingStudentsByPhone.TryGetValue(normalizedStudentPhone, out var sPhone) && (!isUpdate || sPhone.StudentId != studentId))
                        {
                            response.Errors.Add($"{rowPrefix}: SĐT học sinh đã tồn tại.");
                            response.ErrorCount++;
                            continue;
                        }
                    }

                    var normalizedParentPhone = NormalizeOptionalPhoneNumber(parentPhone);
                    if (!string.IsNullOrWhiteSpace(normalizedParentPhone) && !IsValidVietnamPhone(normalizedParentPhone))
                    {
                        response.Errors.Add($"{rowPrefix}: SĐT phụ huynh không hợp lệ.");
                        response.ErrorCount++;
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(parentEmail) && existingEmailsSet.Contains(parentEmail))
                    {
                        if (string.IsNullOrWhiteSpace(normalizedParentPhone) || !existingUserPhones.TryGetValue(normalizedParentPhone, out var p) || !string.Equals(p.Email, parentEmail, StringComparison.OrdinalIgnoreCase))
                        {
                            response.Errors.Add($"{rowPrefix}: Email phụ huynh ({parentEmail}) đã được sử dụng bởi một tài khoản khác.");
                            response.ErrorCount++;
                            continue;
                        }
                    }

                    User? parent = null;
                    bool isNewParent = false;
                    bool updateParent = !string.IsNullOrWhiteSpace(normalizedParentPhone);

                    if (updateParent || !isUpdate)
                    {
                        if (existingUserPhones.TryGetValue(normalizedParentPhone, out var existingParent))
                        {
                            parent = existingParent;
                            if (parent.RoleId != parentRoleId.Value)
                            {
                                response.Errors.Add($"{rowPrefix}: SĐT phụ huynh đang thuộc về tài khoản không phải phụ huynh.");
                                response.ErrorCount++;
                                continue;
                            }

                            bool parentChanged = false;
                            if (!string.IsNullOrWhiteSpace(parentFullName) && parent.FullName != parentFullName)
                            {
                                parent.FullName = parentFullName;
                                parentChanged = true;
                            }
                            if (!string.IsNullOrWhiteSpace(parentEmail) && parent.Email != parentEmail)
                            {
                                parent.Email = parentEmail;
                                parentChanged = true;
                            }
                            if (parentChanged) _context.Users.Update(parent);

                            if (!parent.CenterUsers.Any(cu => cu.CenterId == centerId.Value))
                            {
                                var newCenterUser = new CenterUser { CenterId = centerId.Value, UserId = parent.UserId, UserType = "PARENT", Status = "Active", CreatedAt = now };
                                _context.CenterUsers.Add(newCenterUser);
                                parent.CenterUsers.Add(newCenterUser);
                            }
                        }
                        else
                        {
                            parent = new User
                            {
                                RoleId = parentRoleId.Value,
                                FullName = string.IsNullOrWhiteSpace(parentFullName) ? $"Phụ huynh {fullName}" : parentFullName,
                                Email = string.IsNullOrWhiteSpace(parentEmail) ? null : parentEmail,
                                PhoneNumber = parentPhone,
                                NormalizedPhoneNumber = normalizedParentPhone,
                                PasswordHash = BCrypt.Net.BCrypt.HashPassword("edubridge2026"),
                                EmailConfirmed = true,
                                Status = "Active",
                                IsDeleted = false,
                                CreatedAt = now
                            };
                            isNewParent = true;
                        }
                    }

                    try 
                    {
                        if (isNewParent && parent != null)
                        {
                            _context.Users.Add(parent);
                            await _context.SaveChangesAsync(cancellationToken);
                            existingUserPhones[normalizedParentPhone] = parent;

                            _context.CenterUsers.Add(new CenterUser { CenterId = centerId.Value, UserId = parent.UserId, UserType = "PARENT", Status = "Active", CreatedAt = now });
                            await _context.SaveChangesAsync(cancellationToken);
                        }
                        else if (parent != null && _context.Entry(parent).State == EntityState.Modified)
                        {
                            await _context.SaveChangesAsync(cancellationToken);
                        }

                        if (isUpdate && existingStudentToUpdate != null)
                        {
                            var oldCode = existingStudentToUpdate.StudentCode.ToLower();
                            if (!string.IsNullOrWhiteSpace(studentCode) && oldCode != studentCode.ToLower()) existingStudentsByCode.Remove(oldCode);
                            
                            var oldPhone = existingStudentToUpdate.NormalizedPhoneNumber;
                            if (!string.IsNullOrWhiteSpace(normalizedStudentPhone) && oldPhone != null && oldPhone != normalizedStudentPhone) existingStudentsByPhone.Remove(oldPhone);

                            if (parent != null) existingStudentToUpdate.ParentUserId = parent.UserId;
                            if (!string.IsNullOrWhiteSpace(studentCode)) existingStudentToUpdate.StudentCode = studentCode;
                            if (!string.IsNullOrWhiteSpace(fullName)) existingStudentToUpdate.FullName = fullName;
                            if (dateOfBirth != null) existingStudentToUpdate.DateOfBirth = dateOfBirth;
                            if (!string.IsNullOrWhiteSpace(gender)) existingStudentToUpdate.Gender = gender;
                            if (!string.IsNullOrWhiteSpace(address)) existingStudentToUpdate.Address = address;
                            if (!string.IsNullOrWhiteSpace(studentPhone)) existingStudentToUpdate.PhoneNumber = studentPhone;
                            if (!string.IsNullOrWhiteSpace(studentEmail)) existingStudentToUpdate.Email = studentEmail;
                            if (!string.IsNullOrWhiteSpace(normalizedStudentPhone)) existingStudentToUpdate.NormalizedPhoneNumber = normalizedStudentPhone;

                            _context.Students.Update(existingStudentToUpdate);
                            if (!string.IsNullOrWhiteSpace(studentCode)) existingStudentsByCode[studentCode.ToLower()] = existingStudentToUpdate;
                            if (!string.IsNullOrWhiteSpace(normalizedStudentPhone)) existingStudentsByPhone[normalizedStudentPhone] = existingStudentToUpdate;
                        }
                        else
                        {
                            var student = new Student
                            {
                                ParentUserId = parent.UserId,
                                CenterId = centerId.Value,
                                StudentCode = studentCode,
                                FullName = fullName,
                                DateOfBirth = dateOfBirth,
                                Gender = gender,
                                Address = string.IsNullOrWhiteSpace(address) ? null : address,
                                PhoneNumber = string.IsNullOrWhiteSpace(studentPhone) ? null : studentPhone,
                                Email = string.IsNullOrWhiteSpace(studentEmail) ? null : studentEmail,
                                NormalizedPhoneNumber = normalizedStudentPhone,
                                Status = "Active",
                                IsDeleted = false,
                                CreatedAt = now
                            };

                            _context.Students.Add(student);
                            existingStudentsByCode[studentCode.ToLower()] = student;
                            if (normalizedStudentPhone != null) existingStudentsByPhone[normalizedStudentPhone] = student;
                        }

                        await _context.SaveChangesAsync(cancellationToken);

                        response.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        var inner = ex;
                        while (inner.InnerException != null) inner = inner.InnerException;
                        response.Errors.Add($"{rowPrefix}: Lỗi khi lưu vào DB ({inner.Message}).");
                        response.ErrorCount++;
                        _context.ChangeTracker.Clear();
                    }
                }
                catch (Exception ex)
                {
                    response.Errors.Add($"Dòng {rowIndex}: Lỗi xử lý dữ liệu ({ex.Message}).");
                    response.ErrorCount++;
                    _context.ChangeTracker.Clear();
                }
                finally
                {
                    if (!isEmptyRow)
                    {
                        actualLastRow = Math.Max(actualLastRow, rowIndex);
                        if (response.ErrorCount > initialErrorCount)
                        {
                            row.Cell(13).Value = "Thất bại";
                            var newErrors = response.Errors.Skip(response.Errors.Count - (response.ErrorCount - initialErrorCount)).ToList();
                            row.Cell(14).Value = string.Join(" | ", newErrors.Select(e => e.Contains(':') ? e.Substring(e.IndexOf(':') + 1).Trim() : e));
                        }
                        else
                        {
                            row.Cell(13).Value = "Thành công";
                            row.Cell(14).Value = "";
                        }
                    }
                }
            }

            if (actualLastRow > 0)
            {
                int maxRow = worksheet.LastRowUsed()?.RowNumber() ?? actualLastRow;
                if (maxRow > actualLastRow)
                {
                    worksheet.Rows(actualLastRow + 1, maxRow).Delete();
                }
            }

            worksheet.Cell(1, 13).Value = "Trạng thái";
            worksheet.Cell(1, 14).Value = "Lý do lỗi";
            
            // Try to copy style from column 12 header
            var headerStyle = worksheet.Cell(1, 12).Style;
            worksheet.Cell(1, 13).Style = headerStyle;
            worksheet.Cell(1, 14).Style = headerStyle;

            var resultRange = worksheet.Range(1, 1, actualLastRow, 14);
            resultRange.Style.Font.FontName = "Times New Roman";
            resultRange.Style.Font.FontSize = 12;
            resultRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            resultRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            worksheet.Columns(1, 14).AdjustToContents();
            for (int i = 1; i <= 14; i++)
            {
                worksheet.Column(i).Width += 2;
            }

            using var resultStream = new System.IO.MemoryStream();
            workbook.SaveAs(resultStream);
            resultStream.Position = 0;
            var resultFileName = $"result_{importFile.FileName}";
            resultFileUrl = await _storageService.SaveFileAsync(resultStream, resultFileName, "imports/results", cancellationToken);
        }
        catch (Exception ex)
        {
            await _historyService.UpdateHistoryStatusAsync(history.HistoryId, "Failed", null, cancellationToken);
            return ClassOperationResult<ImportResultResponse>.Failure($"Lỗi đọc file Excel: File không hợp lệ hoặc bị lỗi ({ex.Message}).");
        }

        await _historyService.UpdateHistoryStatusAsync(history.HistoryId, "Completed", resultFileUrl, cancellationToken);

        return ClassOperationResult<ImportResultResponse>.Success(response, "Import hoàn tất.");
    }
}
