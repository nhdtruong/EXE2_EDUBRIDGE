using System.Data;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using EduBridge.Contracts.Parents;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Services.Classes;
using Microsoft.EntityFrameworkCore;
using EduBridge.Services.Auth;
using ClosedXML.Excel;

namespace EduBridge.Services.Parents;

public sealed class ParentManagementService : IParentManagementService
{
    private static readonly int[] AllowedPageSizes = [10, 20, 50, 100, 200, 500];
    private readonly AppDbContext _context;
    private readonly ILogger<ParentManagementService> _logger;
    private readonly ICurrentCenterService _currentCenterService;
    private readonly EduBridge.Services.ImportExportHistories.IImportExportHistoryService _historyService;
    private readonly EduBridge.Services.Storage.IFileStorageService _storageService;

    public ParentManagementService(AppDbContext context, ILogger<ParentManagementService> logger, ICurrentCenterService currentCenterService, EduBridge.Services.ImportExportHistories.IImportExportHistoryService historyService, EduBridge.Services.Storage.IFileStorageService storageService)
    {
        _context = context;
        _logger = logger;
        _currentCenterService = currentCenterService;
        _historyService = historyService;
        _storageService = storageService;
    }

    public async Task<ClassOperationResult<ParentPagedResponse>> GetParentsAsync(
        int ownerUserId, ParentQuery query, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<ParentPagedResponse>("Không tìm thấy trung tâm đang hoạt động.");

        query.Page = Math.Max(1, query.Page);
        query.PageSize = AllowedPageSizes.Contains(query.PageSize) ? query.PageSize : 20;
        query.Name = NormalizeOptional(query.Name);
        query.Email = NormalizeOptional(query.Email)?.ToLowerInvariant();
        query.PhoneNumber = NormalizeOptional(query.PhoneNumber);
        query.Status = NormalizeOptional(query.Status);
        if (query.Status is not null and not ("Active" or "Inactive"))
            return Fail<ParentPagedResponse>("Trạng thái lọc không hợp lệ.", "Status");

        var parents = _context.CenterUsers.AsNoTracking()
            .Where(cu => cu.CenterId == centerId && cu.UserType == "PARENT" && !cu.User.IsDeleted);

        if (query.Name != null) parents = parents.Where(cu => cu.User.FullName.Contains(query.Name));
        if (query.Email != null) parents = parents.Where(cu => cu.User.Email != null && cu.User.Email.Contains(query.Email));
        if (query.PhoneNumber != null)
        {
            var phone = NormalizePhone(query.PhoneNumber);
            parents = parents.Where(cu =>
                (cu.User.PhoneNumber != null && cu.User.PhoneNumber.Contains(query.PhoneNumber)) ||
                (cu.User.NormalizedPhoneNumber != null && cu.User.NormalizedPhoneNumber.Contains(phone)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status)) parents = parents.Where(cu => cu.Status == query.Status);
        if (query.HasChildren == true) parents = parents.Where(cu => cu.User.Students.Any(s => s.CenterId == centerId && !s.IsDeleted));
        if (query.HasChildren == false) parents = parents.Where(cu => !cu.User.Students.Any(s => s.CenterId == centerId && !s.IsDeleted));

        var total = await parents.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)query.PageSize));
        query.Page = Math.Min(query.Page, totalPages);

        var items = await parents
            .OrderByDescending(cu => cu.UserId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(cu => new ParentListItemResponse(
                cu.UserId, cu.User.FullName, cu.User.PhoneNumber, cu.User.Email, cu.Status, cu.User.CreatedAt,
                cu.User.Students.Count(s => s.CenterId == centerId && !s.IsDeleted),
                cu.User.Students.Where(s => s.CenterId == centerId && !s.IsDeleted)
                    .OrderBy(s => s.FullName).Select(s => s.FullName).Take(3).ToList()))
            .ToListAsync(cancellationToken);

        return ClassOperationResult<ParentPagedResponse>.Success(
            new ParentPagedResponse(items, query.Page, query.PageSize, total, totalPages), "Tải danh sách phụ huynh thành công.");
    }

    public async Task<ClassOperationResult<ParentDetailResponse>> GetParentAsync(
        int ownerUserId, int parentUserId, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<ParentDetailResponse>("Không tìm thấy trung tâm đang hoạt động.");

        var parent = await ParentMemberships(centerId.Value)
            .Where(cu => cu.UserId == parentUserId)
            .Select(cu => new ParentDetailResponse(
                cu.UserId, cu.User.FullName, cu.User.PhoneNumber, cu.User.Email, cu.User.DateOfBirth,
                cu.User.Gender, cu.User.IdentityNumber, cu.User.IdentityIssuedDate, cu.User.IdentityIssuedPlace,
                cu.User.Ethnicity, cu.User.Religion, cu.User.CurrentAddress, cu.User.PermanentAddress, cu.User.Hometown, cu.User.PlaceOfBirth,
                cu.Status, cu.User.CreatedAt,
                cu.User.Students.Where(s => s.CenterId == centerId && !s.IsDeleted)
                    .OrderBy(s => s.FullName)
                    .Select(s => new ParentChildResponse(
                        s.StudentId, s.StudentCode, s.FullName, s.DateOfBirth, s.Gender, s.Status)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return parent == null
            ? Fail<ParentDetailResponse>("Không tìm thấy phụ huynh trong trung tâm.")
            : ClassOperationResult<ParentDetailResponse>.Success(parent, "Tải thông tin phụ huynh thành công.");
    }

    public async Task<ClassOperationResult<ParentMutationResponse>> CreateAsync(
        int ownerUserId, SaveParentRequest request, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<ParentMutationResponse>("Không tìm thấy trung tâm đang hoạt động.");
        var validation = ValidateAndNormalize(request);
        if (validation != null) return validation;

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var phone = NormalizePhone(request.PhoneNumber);
            var existingByPhone = await _context.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => !u.IsDeleted && u.NormalizedPhoneNumber == phone, cancellationToken);
            var existingByEmail = request.Email == null
                ? null
                : await _context.Users.Include(u => u.Role)
                    .FirstOrDefaultAsync(u => !u.IsDeleted && u.Email == request.Email, cancellationToken);

            if (existingByPhone != null && existingByEmail != null && existingByPhone.UserId != existingByEmail.UserId)
                return Fail<ParentMutationResponse>("Số điện thoại và email đang thuộc hai tài khoản khác nhau.");

            if (existingByPhone != null && request.Email != null && existingByPhone.Email != request.Email)
                return Fail<ParentMutationResponse>($"Số điện thoại này đã gắn với email {existingByPhone.Email ?? "khác"}. Vui lòng để trống email nếu muốn liên kết, hoặc nhập đúng email.");

            if (existingByEmail != null && existingByEmail.NormalizedPhoneNumber != phone)
                return Fail<ParentMutationResponse>($"Email này đã gắn với số điện thoại khác. Vui lòng kiểm tra lại.");

            var existing = existingByPhone ?? existingByEmail;

            var created = false;
            if (existing != null && existing.Role.RoleCode != "PARENT")
                return Fail<ParentMutationResponse>("Email hoặc số điện thoại đang thuộc tài khoản không phải phụ huynh.");

            if (existing == null)
            {
                var roleId = await _context.Roles.Where(r => r.RoleCode == "PARENT").Select(r => (int?)r.RoleId).FirstOrDefaultAsync(cancellationToken);
                if (roleId == null) return Fail<ParentMutationResponse>("Database chưa có role PARENT.");
                existing = new User
                {
                    RoleId = roleId.Value, FullName = request.FullName, PhoneNumber = request.PhoneNumber,
                    NormalizedPhoneNumber = phone, Email = request.Email, DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender, IdentityNumber = request.IdentityNumber, CurrentAddress = request.CurrentAddress,
                    IdentityIssuedDate = request.IdentityIssuedDate, IdentityIssuedPlace = request.IdentityIssuedPlace,
                    Ethnicity = request.Ethnicity, Religion = request.Religion,
                    PermanentAddress = request.PermanentAddress, Hometown = request.Hometown, PlaceOfBirth = request.PlaceOfBirth,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("edubridge2026"), EmailConfirmed = true,
                    Status = "Active", IsDeleted = false, CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(existing);
                await _context.SaveChangesAsync(cancellationToken);
                created = true;
            }

            var membership = await _context.CenterUsers.FirstOrDefaultAsync(cu =>
                cu.CenterId == centerId && cu.UserId == existing.UserId && cu.UserType == "PARENT", cancellationToken);
            if (membership != null)
                return Fail<ParentMutationResponse>("Phụ huynh đã thuộc trung tâm hiện tại.");

            _context.CenterUsers.Add(new CenterUser
            {
                CenterId = centerId.Value, UserId = existing.UserId, UserType = "PARENT",
                Status = request.Status, CreatedAt = DateTime.UtcNow
            });
            if (request.Status == "Active") existing.Status = "Active";
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ClassOperationResult<ParentMutationResponse>.Success(
                new ParentMutationResponse(existing.UserId, created, request.Status),
                created ? "Đã tạo tài khoản phụ huynh." : "Đã gắn tài khoản phụ huynh vào trung tâm.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Could not create parent for owner {OwnerUserId}.", ownerUserId);
            return Fail<ParentMutationResponse>("Không thể lưu phụ huynh do dữ liệu trùng hoặc vừa thay đổi.");
        }
    }

    public async Task<ClassOperationResult<ParentMutationResponse>> UpdateAsync(
        int ownerUserId, int parentUserId, SaveParentRequest request, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<ParentMutationResponse>("Không tìm thấy trung tâm đang hoạt động.");
        var validation = ValidateAndNormalize(request);
        if (validation != null) return validation;

        var membership = await _context.CenterUsers.Include(cu => cu.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(cu => cu.CenterId == centerId && cu.UserId == parentUserId && cu.UserType == "PARENT" && !cu.User.IsDeleted, cancellationToken);
        if (membership == null) return Fail<ParentMutationResponse>("Không tìm thấy phụ huynh trong trung tâm.");

        var phone = NormalizePhone(request.PhoneNumber);
        var duplicateUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => !u.IsDeleted && u.UserId != parentUserId &&
            (u.NormalizedPhoneNumber == phone || (request.Email != null && u.Email == request.Email) ||
             (request.IdentityNumber != null && u.IdentityNumber == request.IdentityNumber)), cancellationToken);

        if (duplicateUser != null)
        {
            if (duplicateUser.NormalizedPhoneNumber == phone) return Fail<ParentMutationResponse>("Số điện thoại đã tồn tại trong hệ thống.", "PhoneNumber");
            if (request.Email != null && duplicateUser.Email == request.Email) return Fail<ParentMutationResponse>("Email đã tồn tại trong hệ thống.", "Email");
            if (request.IdentityNumber != null && duplicateUser.IdentityNumber == request.IdentityNumber) return Fail<ParentMutationResponse>("CCCD/CMND đã tồn tại trong hệ thống.", "IdentityNumber");
        }

        membership.User.FullName = request.FullName;
        membership.User.PhoneNumber = request.PhoneNumber;
        membership.User.NormalizedPhoneNumber = phone;
        membership.User.Email = request.Email;
        membership.User.DateOfBirth = request.DateOfBirth;
        membership.User.Gender = request.Gender;
        membership.User.IdentityNumber = request.IdentityNumber;
        membership.User.IdentityIssuedDate = request.IdentityIssuedDate;
        membership.User.IdentityIssuedPlace = request.IdentityIssuedPlace;
        membership.User.Ethnicity = request.Ethnicity;
        membership.User.Religion = request.Religion;
        membership.User.CurrentAddress = request.CurrentAddress;
        membership.User.PermanentAddress = request.PermanentAddress;
        membership.User.Hometown = request.Hometown;
        membership.User.PlaceOfBirth = request.PlaceOfBirth;
        membership.Status = request.Status;
        if (request.Status == "Active")
        {
            membership.User.Status = "Active";
        }
        else
        {
            var otherActive = await _context.CenterUsers.AsNoTracking().AnyAsync(cu =>
                cu.UserId == parentUserId &&
                cu.Status == "Active" &&
                cu.CenterUserId != membership.CenterUserId,
                cancellationToken);
            membership.User.Status = otherActive ? "Active" : "Inactive";
        }
        await _context.SaveChangesAsync(cancellationToken);
        return ClassOperationResult<ParentMutationResponse>.Success(
            new ParentMutationResponse(parentUserId, false, request.Status), "Đã cập nhật phụ huynh.");
    }

    public async Task<ClassOperationResult<ParentMutationResponse>> SetStatusAsync(
        int ownerUserId, int parentUserId, string status, CancellationToken cancellationToken = default)
    {
        if (status is not ("Active" or "Inactive")) return Fail<ParentMutationResponse>("Trạng thái không hợp lệ.", "Status");
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        var membership = centerId == null ? null : await _context.CenterUsers.Include(cu => cu.User)
            .FirstOrDefaultAsync(cu => cu.CenterId == centerId && cu.UserId == parentUserId && cu.UserType == "PARENT", cancellationToken);
        if (membership == null) return Fail<ParentMutationResponse>("Không tìm thấy phụ huynh trong trung tâm.");
        membership.Status = status;
        if (status == "Active") membership.User.Status = "Active";
        else
        {
            var otherActive = await _context.CenterUsers.AsNoTracking().AnyAsync(cu =>
                cu.UserId == parentUserId && cu.Status == "Active" && cu.CenterUserId != membership.CenterUserId, cancellationToken);
            membership.User.Status = otherActive ? "Active" : "Inactive";
        }
        await _context.SaveChangesAsync(cancellationToken);
        return ClassOperationResult<ParentMutationResponse>.Success(new(parentUserId, false, status), "Đã cập nhật trạng thái phụ huynh.");
    }

    public async Task<ClassOperationResult<bool>> LinkStudentAsync(
        int ownerUserId, int parentUserId, int studentId, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<bool>("Không tìm thấy trung tâm đang hoạt động.");
        var parentExists = await ParentMemberships(centerId.Value).AnyAsync(cu => cu.UserId == parentUserId, cancellationToken);
        var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId && s.CenterId == centerId && !s.IsDeleted, cancellationToken);
        if (!parentExists || student == null) return Fail<bool>("Phụ huynh hoặc học sinh không thuộc trung tâm hiện tại.");
        student.ParentUserId = parentUserId;
        await _context.SaveChangesAsync(cancellationToken);
        return ClassOperationResult<bool>.Success(true, "Đã liên kết học sinh với phụ huynh.");
    }

    public async Task<ClassOperationResult<IReadOnlyList<LinkableStudentResponse>>> GetLinkableStudentsAsync(
        int ownerUserId, int parentUserId, string? keyword = null, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null || !await ParentMemberships(centerId.Value).AnyAsync(cu => cu.UserId == parentUserId, cancellationToken))
            return Fail<IReadOnlyList<LinkableStudentResponse>>("Không tìm thấy phụ huynh trong trung tâm.");
        keyword = NormalizeOptional(keyword);
        var query = _context.Students.AsNoTracking().Where(s => s.CenterId == centerId && !s.IsDeleted && s.ParentUserId != parentUserId);
        if (keyword != null) query = query.Where(s => s.StudentCode.Contains(keyword) || s.FullName.Contains(keyword));
        var students = await query.OrderBy(s => s.FullName).Take(50)
            .Select(s => new LinkableStudentResponse(s.StudentId, s.StudentCode, s.FullName, s.ParentUser != null ? s.ParentUser.FullName : string.Empty))
            .ToListAsync(cancellationToken);
        return ClassOperationResult<IReadOnlyList<LinkableStudentResponse>>.Success(students, "Tải danh sách học sinh thành công.");
    }

    public async Task<ClassOperationResult<ResetParentPasswordResponse>> ResetPasswordAsync(
        int ownerUserId, int parentUserId, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        var membership = centerId == null ? null : await _context.CenterUsers.Include(cu => cu.User)
            .FirstOrDefaultAsync(cu => cu.CenterId == centerId && cu.UserId == parentUserId && cu.UserType == "PARENT" && !cu.User.IsDeleted, cancellationToken);
        if (membership == null) return Fail<ResetParentPasswordResponse>("Không tìm thấy phụ huynh trong trung tâm.");
        var password = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        membership.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        await _context.SaveChangesAsync(cancellationToken);
        return ClassOperationResult<ResetParentPasswordResponse>.Success(new(parentUserId, password), "Đã cấp lại mật khẩu phụ huynh.");
    }

    public async Task<ClassOperationResult<bool>> DeleteParentAsync(
        int ownerUserId, int parentUserId, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<bool>("Không tìm thấy trung tâm của bạn.");

        var membership = await _context.CenterUsers
            .Include(cu => cu.User)
            .Include(cu => cu.User.Students)
            .FirstOrDefaultAsync(cu => cu.CenterId == centerId && cu.UserId == parentUserId && cu.UserType == "PARENT" && !cu.User.IsDeleted, cancellationToken);

        if (membership == null) return Fail<bool>("Không tìm thấy phụ huynh trong trung tâm.");

        if (membership.User.Students.Any(s => s.CenterId == centerId && !s.IsDeleted && s.Status == "Active"))
        {
            return Fail<bool>("Không thể xóa phụ huynh đang quản lý học sinh ở trạng thái Đang hoạt động.");
        }

        membership.Status = "Inactive";
        membership.User.IsDeleted = true;
        
        await _context.SaveChangesAsync(cancellationToken);
        return ClassOperationResult<bool>.Success(true, "Đã xóa tài khoản phụ huynh thành công.");
    }

    private IQueryable<CenterUser> ParentMemberships(int centerId) =>
        _context.CenterUsers.AsNoTracking().Where(cu => cu.CenterId == centerId && cu.UserType == "PARENT" && !cu.User.IsDeleted);

    private async Task<int?> GetOwnerCenterIdAsync(int ownerUserId, CancellationToken cancellationToken) =>
        await _currentCenterService.GetCenterIdAsync(cancellationToken);

    private static ClassOperationResult<ParentMutationResponse>? ValidateAndNormalize(SaveParentRequest request)
    {
        request.FullName = Regex.Replace(request.FullName?.Trim() ?? "", @"\s+", " ");
        request.PhoneNumber = request.PhoneNumber?.Trim() ?? "";
        request.Email = NormalizeOptional(request.Email)?.ToLowerInvariant();
        request.Gender = NormalizeOptional(request.Gender) ?? "Nam";
        request.IdentityNumber = NormalizeOptional(request.IdentityNumber);
        request.IdentityIssuedPlace = NormalizeOptional(request.IdentityIssuedPlace);
        request.Ethnicity = NormalizeOptional(request.Ethnicity);
        request.Religion = NormalizeOptional(request.Religion);
        request.CurrentAddress = NormalizeOptional(request.CurrentAddress);
        request.PermanentAddress = NormalizeOptional(request.PermanentAddress);
        request.Hometown = NormalizeOptional(request.Hometown);
        request.PlaceOfBirth = NormalizeOptional(request.PlaceOfBirth);
        request.Status = request.Status == "Inactive" ? "Inactive" : "Active";
        var phone = NormalizePhone(request.PhoneNumber);
        if (request.FullName.Length < 2) return Fail<ParentMutationResponse>("Vui lòng nhập tên phụ huynh hợp lệ.", "FullName");
        if (phone.Length is < 10 or > 12 || !phone.StartsWith('0')) return Fail<ParentMutationResponse>("Số điện thoại phải gồm 10-12 số và bắt đầu bằng 0.", "PhoneNumber");
        if (request.Gender is not ("Nam" or "Nữ")) return Fail<ParentMutationResponse>("Giới tính không hợp lệ.", "Gender");
        if (request.IdentityNumber != null && (!request.IdentityNumber.All(char.IsDigit) || request.IdentityNumber.Length is not (9 or 12)))
            return Fail<ParentMutationResponse>("CMND/CCCD phải gồm 9 hoặc 12 số.", "IdentityNumber");
        return null;
    }

    public async Task<ClassOperationResult<ParentImportResultResponse>> ImportParentsAsync(int ownerUserId, Microsoft.AspNetCore.Http.IFormFile importFile, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return Fail<ParentImportResultResponse>("Không tìm thấy trung tâm đang hoạt động.");

        var parentRoleId = await _context.Roles.Where(r => r.RoleCode == "PARENT").Select(r => (int?)r.RoleId).FirstOrDefaultAsync(cancellationToken);
        if (parentRoleId == null) return Fail<ParentImportResultResponse>("Hệ thống chưa cấu hình role PARENT.");

        // 1. Lưu file đầu vào
        var inputFileUrl = await _storageService.SaveFileAsync(importFile, "imports", cancellationToken);

        // 2. Tạo lịch sử Import
        var history = await _historyService.CreateHistoryAsync(new EduBridge.Contracts.ImportExportHistories.CreateImportExportHistoryRequest
        {
            CenterId = centerId.Value,
            UserId = ownerUserId,
            Title = $"Import phụ huynh {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
            ActionType = "Import",
            EntityName = "Parents",
            InputFileUrl = inputFileUrl,
            Status = "Processing"
        }, cancellationToken);

        var result = new ParentImportResultResponse();
        try
        {
            using var stream = importFile.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            
            // Add header for result
            worksheet.Cell(1, 9).Value = "Trạng thái";
            worksheet.Cell(1, 10).Value = "Lý do lỗi";
            worksheet.Cell(1, 9).Style = worksheet.Cell(1, 8).Style;
            worksheet.Cell(1, 10).Style = worksheet.Cell(1, 8).Style;

            var rows = worksheet.RangeUsed().RowsUsed().Skip(1).ToList();

            var emailsInExcel = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var phonesInExcel = new HashSet<string>();

            foreach (var row in rows)
            {
                var idStr = row.Cell(2).GetString().Trim();
                var phone = row.Cell(4).GetString().Trim();
                var email = row.Cell(5).GetString().Trim();

                if (string.IsNullOrWhiteSpace(idStr))
                {
                    if (!string.IsNullOrWhiteSpace(phone)) phonesInExcel.Add(NormalizePhone(phone));
                    if (!string.IsNullOrWhiteSpace(email)) emailsInExcel.Add(email.ToLowerInvariant());
                }
                else if (int.TryParse(idStr, out _))
                {
                    if (!string.IsNullOrWhiteSpace(phone)) phonesInExcel.Add(NormalizePhone(phone));
                    if (!string.IsNullOrWhiteSpace(email)) emailsInExcel.Add(email.ToLowerInvariant());
                }
            }

            var emailsList = emailsInExcel.ToList();
            var existingEmailsSet = new HashSet<string>(await _context.Users
                .Where(u => !u.IsDeleted && u.Email != null && emailsList.Contains(u.Email))
                .Select(u => u.Email!)
                .ToListAsync(cancellationToken), StringComparer.OrdinalIgnoreCase);

            var phonesList = phonesInExcel.ToList();
            var existingPhonesSet = new HashSet<string>(await _context.Users
                .Where(u => !u.IsDeleted && u.NormalizedPhoneNumber != null && phonesList.Contains(u.NormalizedPhoneNumber))
                .Select(u => u.NormalizedPhoneNumber!)
                .ToListAsync(cancellationToken));

            var userIdsList = rows.Select(r => r.Cell(2).GetString().Trim())
                                  .Where(id => !string.IsNullOrWhiteSpace(id) && int.TryParse(id, out _))
                                  .Select(int.Parse).ToList();

            var existingIdsSet = new HashSet<int>(await _context.CenterUsers
                .Where(cu => cu.CenterId == centerId && cu.UserType == "PARENT" && userIdsList.Contains(cu.UserId) && !cu.User.IsDeleted)
                .Select(cu => cu.UserId)
                .ToListAsync(cancellationToken));

            int stt = 0;
            int lastDataRow = 1;
            foreach (var row in rows)
            {
                var idStr = row.Cell(2).GetString().Trim();
                var fullName = row.Cell(3).GetString().Trim();
                var phone = row.Cell(4).GetString().Trim();
                var email = row.Cell(5).GetString().Trim();
                var genderCode = row.Cell(6).GetString().Trim();
                var dobStr = row.Cell(7).GetString().Trim();
                var address = row.Cell(8).GetString().Trim();

                if (string.IsNullOrWhiteSpace(idStr) && string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(email))
                {
                    continue; 
                }

                stt++;
                lastDataRow = Math.Max(lastDataRow, row.RowNumber());

                bool isUpdate = false;
                int existingUserId = 0;

                if (!string.IsNullOrWhiteSpace(idStr))
                {
                    if (int.TryParse(idStr, out var id))
                    {
                        if (!existingIdsSet.Contains(id))
                        {
                            row.Cell(9).Value = "Thất bại";
                            row.Cell(10).Value = "ID không tồn tại trong trung tâm.";
                            result.Errors.Add($"STT {stt}: ID không tồn tại.");
                            result.ErrorCount++;
                            continue;
                        }
                        isUpdate = true;
                        existingUserId = id;
                    }
                    else
                    {
                        row.Cell(9).Value = "Thất bại";
                        row.Cell(10).Value = "ID không hợp lệ.";
                        result.Errors.Add($"STT {stt}: ID không hợp lệ.");
                        result.ErrorCount++;
                        continue;
                    }
                }

                if (!isUpdate)
                {
                    if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone))
                    {
                        row.Cell(9).Value = "Thất bại";
                        row.Cell(10).Value = "Họ tên và SĐT là bắt buộc.";
                        result.Errors.Add($"STT {stt}: Họ tên và SĐT là bắt buộc.");
                        result.ErrorCount++;
                        continue;
                    }
                }

                string? normPhone = null;
                if (!string.IsNullOrWhiteSpace(phone))
                {
                    if (phone.Length < 10 || phone.Length > 12 || !phone.StartsWith('0'))
                    {
                        row.Cell(9).Value = "Thất bại";
                        row.Cell(10).Value = "Số điện thoại không hợp lệ.";
                        result.Errors.Add($"STT {stt}: Số điện thoại không hợp lệ.");
                        result.ErrorCount++;
                        continue;
                    }
                    normPhone = NormalizePhone(phone);

                    if (!isUpdate && existingPhonesSet.Contains(normPhone))
                    {
                        row.Cell(9).Value = "Thất bại";
                        row.Cell(10).Value = "SĐT đã được sử dụng.";
                        result.Errors.Add($"STT {stt}: SĐT đã được sử dụng.");
                        result.ErrorCount++;
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(email))
                {
                    if (!isUpdate && existingEmailsSet.Contains(email.ToLowerInvariant()))
                    {
                        row.Cell(9).Value = "Thất bại";
                        row.Cell(10).Value = "Email đã được sử dụng.";
                        result.Errors.Add($"STT {stt}: Email đã được sử dụng.");
                        result.ErrorCount++;
                        continue;
                    }
                }

                DateOnly? parsedDob = null;
                if (!string.IsNullOrWhiteSpace(dobStr))
                {
                    if (!DateTime.TryParseExact(dobStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var dobVal))
                    {
                        row.Cell(9).Value = "Thất bại";
                        row.Cell(10).Value = "Ngày sinh không đúng định dạng (dd/MM/yyyy).";
                        result.Errors.Add($"STT {stt}: Ngày sinh không đúng định dạng.");
                        result.ErrorCount++;
                        continue;
                    }
                    parsedDob = DateOnly.FromDateTime(dobVal);
                }

                if (isUpdate)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == existingUserId, cancellationToken);
                    
                    if (normPhone != null && user.NormalizedPhoneNumber != normPhone && existingPhonesSet.Contains(normPhone))
                    {
                        row.Cell(9).Value = "Thất bại";
                        row.Cell(10).Value = "SĐT đã được sử dụng bởi người khác.";
                        result.Errors.Add($"STT {stt}: SĐT đã được sử dụng.");
                        result.ErrorCount++;
                        continue;
                    }

                    var lowerEmail = email?.ToLowerInvariant();
                    if (!string.IsNullOrWhiteSpace(lowerEmail) && user.Email?.ToLowerInvariant() != lowerEmail && existingEmailsSet.Contains(lowerEmail))
                    {
                        row.Cell(9).Value = "Thất bại";
                        row.Cell(10).Value = "Email đã được sử dụng bởi người khác.";
                        result.Errors.Add($"STT {stt}: Email đã được sử dụng.");
                        result.ErrorCount++;
                        continue;
                    }

                    if (user.NormalizedPhoneNumber != null) existingPhonesSet.Remove(user.NormalizedPhoneNumber);
                    if (user.Email != null) existingEmailsSet.Remove(user.Email.ToLowerInvariant());

                    if (!string.IsNullOrWhiteSpace(fullName)) user.FullName = fullName;
                    if (!string.IsNullOrWhiteSpace(phone)) user.PhoneNumber = phone;
                    if (normPhone != null) user.NormalizedPhoneNumber = normPhone;
                    if (!string.IsNullOrWhiteSpace(email)) user.Email = email;
                    if (!string.IsNullOrWhiteSpace(genderCode)) user.Gender = genderCode == "1" ? "Nam" : (genderCode == "2" ? "Nữ" : "Nam");
                    if (parsedDob != null) user.DateOfBirth = parsedDob;
                    if (!string.IsNullOrWhiteSpace(address)) user.CurrentAddress = address;

                    if (user.NormalizedPhoneNumber != null) existingPhonesSet.Add(user.NormalizedPhoneNumber);
                    if (!string.IsNullOrWhiteSpace(user.Email)) existingEmailsSet.Add(user.Email.ToLowerInvariant());

                    row.Cell(9).Value = "Thành công";
                    row.Cell(10).Value = "";
                    result.SuccessCount++;
                }
                else
                {
                    var newUser = new User
                    {
                        FullName = fullName,
                        PhoneNumber = phone,
                        NormalizedPhoneNumber = normPhone,
                        Email = string.IsNullOrWhiteSpace(email) ? null : email,
                        Gender = genderCode == "1" ? "Nam" : (genderCode == "2" ? "Nữ" : "Nam"),
                        DateOfBirth = parsedDob,
                        CurrentAddress = string.IsNullOrWhiteSpace(address) ? null : address,
                        RoleId = parentRoleId.Value,
                        PasswordHash = "edubridge2026",
                        EmailConfirmed = true,
                        Status = "Active",
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(newUser);
                    _context.CenterUsers.Add(new CenterUser
                    {
                        CenterId = centerId.Value,
                        User = newUser,
                        UserType = "PARENT",
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    });

                    existingPhonesSet.Add(normPhone);
                    if (!string.IsNullOrWhiteSpace(email)) existingEmailsSet.Add(email.ToLowerInvariant());

                    row.Cell(9).Value = "Thành công";
                    row.Cell(10).Value = "";
                    result.SuccessCount++;
                }
            }

            if (result.SuccessCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            // 3. Ghi file kết quả
            // Xóa các ghi chú và mô tả (nếu có) ngoài vùng dữ liệu
            int lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 10;
            if (lastCol > 10)
            {
                worksheet.Columns(11, lastCol).Delete();
            }
            
            if (lastDataRow > 0)
            {
                int maxRow = worksheet.LastRowUsed()?.RowNumber() ?? lastDataRow;
                if (maxRow > lastDataRow)
                {
                    worksheet.Rows(lastDataRow + 1, maxRow).Delete();
                }
            }

            var resultRange = worksheet.Range(1, 1, lastDataRow, 10);
            resultRange.Style.Font.FontName = "Times New Roman";
            resultRange.Style.Font.FontSize = 12;
            resultRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            resultRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            worksheet.Columns(1, 10).AdjustToContents();
            for (int i = 1; i <= 10; i++)
            {
                worksheet.Column(i).Width += 2;
            }

            using var resultStream = new System.IO.MemoryStream();
            workbook.SaveAs(resultStream);
            resultStream.Position = 0;
            var resultFileName = $"result_{importFile.FileName}";
            var resultFileUrl = await _storageService.SaveFileAsync(resultStream, resultFileName, "imports/results", cancellationToken);

            // 4. Cập nhật lịch sử
            await _historyService.UpdateHistoryStatusAsync(history.HistoryId, "Completed", resultFileUrl, cancellationToken);

            return ClassOperationResult<ParentImportResultResponse>.Success(result, "Import hoàn tất.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi import phụ huynh");
            if (history != null) {
                await _historyService.UpdateHistoryStatusAsync(history.HistoryId, "Failed", null, cancellationToken);
            }
            return Fail<ParentImportResultResponse>($"Đã xảy ra lỗi khi đọc file Excel: {ex.Message}{(ex.InnerException != null ? " - " + ex.InnerException.Message : "")}");
        }
    }

    private static string NormalizePhone(string value)
    {
        var digits = new string((value ?? "").Where(char.IsDigit).ToArray());
        return digits.StartsWith("84") && digits.Length > 9 ? $"0{digits[2..]}" : digits;
    }
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static ClassOperationResult<T> Fail<T>(string message, string key = "") =>
        ClassOperationResult<T>.Failure(message, new Dictionary<string, string[]> { [key] = [message] });
}


