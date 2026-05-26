using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using EduBridge.Data;
using EduBridge.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Pages
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminTeachersModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminTeachersModel> _logger;

        public AdminTeachersModel(
            AppDbContext context,
            ILogger<AdminTeachersModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        [MaxLength(150)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        [MaxLength(150)]
        public string? ContactSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        [MaxLength(20)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 20;

        [BindProperty]
        public TeacherInput Input { get; set; } = new();

        [BindProperty]
        public TeacherEditInput EditInput { get; set; } = new();

        public List<TeacherListItem> Teachers { get; private set; } = new();

        public int[] PageSizeOptions { get; } = { 10, 20, 50, 100, 200, 500 };

        public int TotalTeachers { get; private set; }

        public int TotalPages => TotalTeachers == 0
            ? 1
            : (int)Math.Ceiling(TotalTeachers / (double)PageSize);

        public int FirstItemNumber => TotalTeachers == 0
            ? 0
            : (PageNumber - 1) * PageSize + 1;

        public string? ResetPasswordTeacherName => TempData["ResetPasswordTeacherName"] as string;

        public string? ResetPasswordValue => TempData["ResetPasswordValue"] as string;

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();

            if (ownerUserId == null)
            {
                return RedirectToPage("/Login");
            }

            var centerId = await GetOwnerCenterIdAsync(ownerUserId.Value, cancellationToken);

            if (centerId == null)
            {
                _logger.LogWarning(
                    "Owner user {OwnerUserId} has no active center for teacher management.",
                    ownerUserId.Value);

                Teachers = new();
                return Page();
            }

            NormalizeFilters();

            if (!ValidateFilters())
            {
                Teachers = new();
                return Page();
            }

            await LoadTeachersAsync(centerId.Value, cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnGetDetailAsync(
            int teacherId,
            CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();

            if (ownerUserId == null)
            {
                return new UnauthorizedResult();
            }

            var centerId = await GetOwnerCenterIdAsync(ownerUserId.Value, cancellationToken);

            if (centerId == null)
            {
                return new ForbidResult();
            }

            var teacher = await _context.Teachers
                .AsNoTracking()
                .Where(t => t.TeacherId == teacherId && t.CenterId == centerId.Value)
                .Select(t => new
                {
                    t.TeacherId,
                    t.TeacherCode,
                    t.User.FullName,
                    t.User.DateOfBirth,
                    t.User.PhoneNumber,
                    t.User.Email,
                    t.User.Gender,
                    t.User.Address,
                    t.User.AvatarUrl
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (teacher == null)
            {
                return new NotFoundResult();
            }

            return new JsonResult(new
            {
                teacher.TeacherId,
                teacher.TeacherCode,
                teacher.FullName,
                dateOfBirth = teacher.DateOfBirth?.ToString("yyyy-MM-dd"),
                teacher.PhoneNumber,
                teacher.Email,
                gender = string.IsNullOrWhiteSpace(teacher.Gender) ? "Nam" : teacher.Gender,
                teacher.Address,
                teacher.AvatarUrl
            });
        }

        public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();

            if (ownerUserId == null)
            {
                return RedirectToPage("/Login");
            }

            var centerId = await GetOwnerCenterIdAsync(ownerUserId.Value, cancellationToken);

            if (centerId == null)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy trung tâm đang hoạt động.");
                await LoadPageDataAsync(null, cancellationToken);
                return Page();
            }

            Input.Normalize();
            ModelState.Clear();
            TryValidateModel(Input, nameof(Input));
            await ValidateTeacherInputAsync(centerId.Value, cancellationToken);

            if (!ModelState.IsValid)
            {
                LogInvalidModelState("Create teacher");
                AddModelStateSummaryError("Không thể thêm giáo viên. Vui lòng kiểm tra lại thông tin giáo viên.");
                await LoadPageDataAsync(centerId.Value, cancellationToken);
                return Page();
            }

            var normalizedPhone = NormalizePhoneNumber(Input.PhoneNumber);

            var teacherRoleId = await _context.Roles
                .AsNoTracking()
                .Where(r => r.RoleCode == "TEACHER")
                .Select(r => (int?)r.RoleId)
                .FirstOrDefaultAsync(cancellationToken);

            if (teacherRoleId == null)
            {
                ModelState.AddModelError(string.Empty, "Chưa cấu hình role TEACHER.");
                await LoadPageDataAsync(centerId.Value, cancellationToken);
                return Page();
            }

            string? uploadedAvatarUrl = null;

            try
            {
                uploadedAvatarUrl = await SaveAvatarAsync(
                    Input.AvatarFile,
                    Input.TeacherCode,
                    cancellationToken);

                await using var transaction = await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

                var user = new User
                {
                    RoleId = teacherRoleId.Value,
                    FullName = Input.FullName,
                    Email = Input.Email,
                    PhoneNumber = Input.PhoneNumber,
                    NormalizedPhoneNumber = normalizedPhone,
                    DateOfBirth = Input.DateOfBirth,
                    Gender = NormalizeNullable(Input.Gender),
                    Address = NormalizeNullable(Input.Address),
                    AvatarUrl = uploadedAvatarUrl,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("edubridge2026"),
                    EmailConfirmed = true,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync(cancellationToken);

                _context.Teachers.Add(new EduBridge.Models.Teacher
                {
                    UserId = user.UserId,
                    CenterId = centerId.Value,
                    TeacherCode = Input.TeacherCode,
                    Status = "Active",
                    Specialization = null,
                    ExperienceYears = 0
                });

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                DeleteUploadedAvatar(uploadedAvatarUrl);
                _logger.LogWarning(ex, "Could not create teacher {TeacherCode}.", Input.TeacherCode);
                ModelState.AddModelError(string.Empty, "Không thể thêm giáo viên. Vui lòng kiểm tra dữ liệu trùng.");
                await LoadPageDataAsync(centerId.Value, cancellationToken);
                return Page();
            }

            TempData["ToastType"] = "success";
            TempData["ToastTitle"] = "Thành công";
            TempData["ToastMessage"] = "Đã thêm giáo viên mới.";

            return RedirectToPage("/AdminTeachers");
        }

        public async Task<IActionResult> OnPostUpdateAsync(CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();

            if (ownerUserId == null)
            {
                return RedirectToPage("/Login");
            }

            var centerId = await GetOwnerCenterIdAsync(ownerUserId.Value, cancellationToken);

            if (centerId == null)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy trung tâm đang hoạt động.");
                await LoadPageDataAsync(null, cancellationToken);
                return Page();
            }

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(
                    t => t.TeacherId == EditInput.TeacherId && t.CenterId == centerId.Value,
                    cancellationToken);

            if (teacher == null)
            {
                return NotFound();
            }

            EditInput.Normalize();
            ModelState.Clear();
            TryValidateModel(EditInput, nameof(EditInput));

            _logger.LogInformation(
                "Update teacher submit: TeacherId={TeacherId}, TeacherCode={TeacherCode}, FullName={FullName}, Phone={Phone}, Email={Email}, DateOfBirth={DateOfBirth}, Gender={Gender}",
                EditInput.TeacherId,
                EditInput.TeacherCode,
                EditInput.FullName,
                EditInput.PhoneNumber,
                EditInput.Email,
                EditInput.DateOfBirth,
                EditInput.Gender);

            await ValidateTeacherInputAsync(
                centerId.Value,
                EditInput,
                teacher.TeacherId,
                teacher.UserId,
                cancellationToken);

            if (!ModelState.IsValid)
            {
                LogInvalidModelState("Update teacher");
                AddModelStateSummaryError("Không thể lưu thay đổi. Vui lòng kiểm tra lại thông tin giáo viên.");
                await LoadPageDataAsync(centerId.Value, cancellationToken);
                return Page();
            }

            var normalizedPhone = NormalizePhoneNumber(EditInput.PhoneNumber);
            var oldAvatarUrl = teacher.User.AvatarUrl;
            string? uploadedAvatarUrl = null;

            try
            {
                uploadedAvatarUrl = await SaveAvatarAsync(
                    EditInput.AvatarFile,
                    EditInput.TeacherCode,
                    cancellationToken);

                teacher.TeacherCode = EditInput.TeacherCode;
                teacher.User.FullName = EditInput.FullName;
                teacher.User.Email = EditInput.Email;
                teacher.User.PhoneNumber = EditInput.PhoneNumber;
                teacher.User.NormalizedPhoneNumber = normalizedPhone;
                teacher.User.DateOfBirth = EditInput.DateOfBirth;
                teacher.User.Gender = NormalizeNullable(EditInput.Gender);
                teacher.User.Address = NormalizeNullable(EditInput.Address);

                if (EditInput.RemoveAvatar)
                {
                    teacher.User.AvatarUrl = null;
                }

                if (!string.IsNullOrWhiteSpace(uploadedAvatarUrl))
                {
                    teacher.User.AvatarUrl = uploadedAvatarUrl;
                }

                await _context.SaveChangesAsync(cancellationToken);

                if (!string.Equals(oldAvatarUrl, teacher.User.AvatarUrl, StringComparison.OrdinalIgnoreCase))
                {
                    DeleteUploadedAvatar(oldAvatarUrl);
                }
            }
            catch (DbUpdateException ex)
            {
                DeleteUploadedAvatar(uploadedAvatarUrl);
                _logger.LogWarning(ex, "Could not update teacher {TeacherId}.", teacher.TeacherId);
                ModelState.AddModelError(string.Empty, "Không thể cập nhật giáo viên. Vui lòng kiểm tra dữ liệu trùng.");
                await LoadPageDataAsync(centerId.Value, cancellationToken);
                return Page();
            }

            TempData["ToastType"] = "success";
            TempData["ToastTitle"] = "Thành công";
            TempData["ToastMessage"] = "Đã cập nhật thông tin giáo viên.";

            return RedirectToPage("/AdminTeachers", new { Search, ContactSearch, StatusFilter, PageNumber, PageSize });
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(
            int teacherId,
            CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();

            if (ownerUserId == null)
            {
                return RedirectToPage("/Login");
            }

            var centerId = await GetOwnerCenterIdAsync(ownerUserId.Value, cancellationToken);

            if (centerId == null)
            {
                return Forbid();
            }

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(
                    t => t.TeacherId == teacherId && t.CenterId == centerId.Value,
                    cancellationToken);

            if (teacher == null)
            {
                return NotFound();
            }

            var nextStatus = teacher.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)
                ? "Inactive"
                : "Active";

            teacher.Status = nextStatus;
            teacher.User.Status = nextStatus;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["ToastType"] = "success";
            TempData["ToastTitle"] = "Thành công";
            TempData["ToastMessage"] = nextStatus == "Active"
                ? "Đã kích hoạt giáo viên."
                : "Đã tạm dừng giáo viên.";

            return RedirectToPage("/AdminTeachers", new { Search, ContactSearch, StatusFilter, PageNumber, PageSize });
        }

        public async Task<IActionResult> OnPostResetPasswordAsync(
            int teacherId,
            CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();

            if (ownerUserId == null)
            {
                return RedirectToPage("/Login");
            }

            var centerId = await GetOwnerCenterIdAsync(ownerUserId.Value, cancellationToken);

            if (centerId == null)
            {
                return Forbid();
            }

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(
                    t => t.TeacherId == teacherId && t.CenterId == centerId.Value,
                    cancellationToken);

            if (teacher == null)
            {
                return NotFound();
            }

            var newPassword = GenerateNumericPassword(6);
            teacher.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Owner user {OwnerUserId} reset password for teacher {TeacherId}.",
                ownerUserId.Value,
                teacher.TeacherId);

            TempData["ToastType"] = "success";
            TempData["ToastTitle"] = "Thành công";
            TempData["ToastMessage"] = "Đổi mật khẩu thành công.";
            TempData["ResetPasswordTeacherName"] = teacher.User.FullName;
            TempData["ResetPasswordValue"] = newPassword;

            return RedirectToPage("/AdminTeachers", new { Search, ContactSearch, StatusFilter, PageNumber, PageSize });
        }

        public async Task<IActionResult> OnPostDeleteAsync(
            int teacherId,
            CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();

            if (ownerUserId == null)
            {
                return RedirectToPage("/Login");
            }

            var centerId = await GetOwnerCenterIdAsync(ownerUserId.Value, cancellationToken);

            if (centerId == null)
            {
                return Forbid();
            }

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(
                    t => t.TeacherId == teacherId && t.CenterId == centerId.Value,
                    cancellationToken);

            if (teacher == null)
            {
                return NotFound();
            }

            if (!teacher.Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase) ||
                !teacher.User.Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ToastType"] = "error";
                TempData["ToastTitle"] = "Thất bại";
                TempData["ToastMessage"] = "Chỉ có thể xóa giáo viên đang tạm dừng.";

                return RedirectToPage("/AdminTeachers", new { Search, ContactSearch, StatusFilter, PageNumber, PageSize });
            }

            var hasClasses = await _context.Classes
                .AsNoTracking()
                .AnyAsync(c => c.TeacherId == teacher.TeacherId, cancellationToken);

            if (hasClasses)
            {
                TempData["ToastType"] = "error";
                TempData["ToastTitle"] = "Thất bại";
                TempData["ToastMessage"] = "Không thể xóa giáo viên đã được gắn lớp.";

                return RedirectToPage("/AdminTeachers", new { Search, ContactSearch, StatusFilter, PageNumber, PageSize });
            }

            var oldAvatarUrl = teacher.User.AvatarUrl;

            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                await _context.Notifications
                    .Where(n => n.UserId == teacher.UserId)
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.Messages
                    .Where(m => m.SenderUserId == teacher.UserId || m.ReceiverUserId == teacher.UserId)
                    .ExecuteDeleteAsync(cancellationToken);

                _context.Teachers.Remove(teacher);
                _context.Users.Remove(teacher.User);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                DeleteUploadedAvatar(oldAvatarUrl);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Could not delete teacher {TeacherId}.", teacher.TeacherId);
                TempData["ToastType"] = "error";
                TempData["ToastTitle"] = "Thất bại";
                TempData["ToastMessage"] = "Không thể xóa giáo viên vì dữ liệu đang được liên kết.";

                return RedirectToPage("/AdminTeachers", new { Search, ContactSearch, StatusFilter, PageNumber, PageSize });
            }

            TempData["ToastType"] = "success";
            TempData["ToastTitle"] = "Thành công";
            TempData["ToastMessage"] = "Đã xóa giáo viên.";

            return RedirectToPage("/AdminTeachers", new { Search, ContactSearch, StatusFilter, PageNumber, PageSize });
        }

        private async Task LoadPageDataAsync(
            int? centerId,
            CancellationToken cancellationToken)
        {
            if (centerId == null)
            {
                Teachers = new();
                return;
            }

            await LoadTeachersAsync(centerId.Value, cancellationToken);
        }

        private void LogInvalidModelState(string actionName)
        {
            var errors = ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .Select(entry => new
                {
                    Field = entry.Key,
                    Errors = entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray()
                })
                .ToArray();

            _logger.LogWarning(
                "{ActionName} failed validation: {@ValidationErrors}",
                actionName,
                errors);
        }

        private void AddModelStateSummaryError(string errorMessage)
        {
            if (!ModelState.TryGetValue(string.Empty, out var entry) ||
                !entry.Errors.Any(error => error.ErrorMessage == errorMessage))
            {
                ModelState.AddModelError(string.Empty, errorMessage);
            }
        }

        private void RemoveModelStatePrefix(string prefix)
        {
            var keys = ModelState.Keys
                .Where(key =>
                    key.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
                    key.StartsWith($"{prefix}.", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keys)
            {
                ModelState.Remove(key);
            }
        }

        private async Task LoadTeachersAsync(
            int centerId,
            CancellationToken cancellationToken)
        {
            var query = _context.Teachers
                .AsNoTracking()
                .Where(t => t.CenterId == centerId);

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var keyword = Search.Trim();

                query = query.Where(t =>
                    t.TeacherCode.Contains(keyword) ||
                    t.User.FullName.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(ContactSearch))
            {
                var keyword = ContactSearch.Trim();
                var normalizedPhoneKeyword = NormalizePhoneNumber(keyword);

                query = query.Where(t =>
                    (t.User.Email != null && t.User.Email.Contains(keyword)) ||
                    (t.User.PhoneNumber != null && t.User.PhoneNumber.Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(normalizedPhoneKeyword) &&
                        t.User.NormalizedPhoneNumber != null &&
                        t.User.NormalizedPhoneNumber.Contains(normalizedPhoneKeyword)));
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter))
            {
                query = query.Where(t =>
                    t.Status == StatusFilter &&
                    t.User.Status == StatusFilter);
            }

            TotalTeachers = await query.CountAsync(cancellationToken);

            if (TotalTeachers == 0)
            {
                PageNumber = 1;
                Teachers = new();
                return;
            }

            if (PageNumber > TotalPages)
            {
                PageNumber = TotalPages;
            }

            Teachers = await query
                .OrderByDescending(t => t.TeacherId)
                .Select(t => new TeacherListItem
                {
                    TeacherId = t.TeacherId,
                    TeacherCode = t.TeacherCode,
                    FullName = t.User.FullName,
                    Email = t.User.Email ?? string.Empty,
                    PhoneNumber = t.User.PhoneNumber ?? string.Empty,
                    AvatarUrl = t.User.AvatarUrl,
                    Status = t.Status,
                    UserStatus = t.User.Status,
                    ClassCount = t.Classes.Count(c => c.Status == "Active"),
                    StudentCount = t.Classes
                        .Where(c => c.Status == "Active")
                        .SelectMany(c => c.Enrollments)
                        .Count(e => e.Status == "Đang học")
                })
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync(cancellationToken);
        }

        private void NormalizeFilters()
        {
            Search = NormalizeFilterValue(Search);
            ContactSearch = NormalizeFilterValue(ContactSearch);
            StatusFilter = NormalizeFilterValue(StatusFilter);
            NormalizePagination();
        }

        private void NormalizePagination()
        {
            if (!PageSizeOptions.Contains(PageSize))
            {
                PageSize = 20;
            }

            if (PageNumber < 1)
            {
                PageNumber = 1;
            }
        }

        private bool ValidateFilters()
        {
            var valid = true;

            if (!string.IsNullOrWhiteSpace(Search))
            {
                if (Search.Length > 150)
                {
                    ModelState.AddModelError("Search", "Từ khóa mã/tên tối đa 150 ký tự.");
                    valid = false;
                }

                if (Search.Any(char.IsControl))
                {
                    ModelState.AddModelError("Search", "Từ khóa mã/tên không hợp lệ.");
                    valid = false;
                }
            }

            if (!string.IsNullOrWhiteSpace(ContactSearch))
            {
                if (ContactSearch.Length > 150)
                {
                    ModelState.AddModelError("ContactSearch", "Từ khóa email/SĐT tối đa 150 ký tự.");
                    valid = false;
                }

                if (ContactSearch.Any(char.IsControl))
                {
                    ModelState.AddModelError("ContactSearch", "Từ khóa email/SĐT không hợp lệ.");
                    valid = false;
                }
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter) &&
                StatusFilter is not ("Active" or "Inactive"))
            {
                ModelState.AddModelError("StatusFilter", "Trạng thái lọc không hợp lệ.");
                valid = false;
            }

            return valid;
        }

        private async Task ValidateTeacherInputAsync(
            int centerId,
            CancellationToken cancellationToken)
        {
            await ValidateTeacherInputAsync(
                centerId,
                Input,
                excludedTeacherId: null,
                excludedUserId: null,
                cancellationToken);
        }

        private async Task ValidateTeacherInputAsync(
            int centerId,
            TeacherInputBase input,
            int? excludedTeacherId,
            int? excludedUserId,
            CancellationToken cancellationToken)
        {
            if (!IsValidTeacherCode(input.TeacherCode))
            {
                ModelState.AddModelError(GetFieldName(input, nameof(input.TeacherCode)), "Mã giáo viên chỉ gồm chữ, số, dấu gạch ngang và tối đa 30 ký tự.");
            }

            if (!IsValidFullName(input.FullName))
            {
                ModelState.AddModelError(GetFieldName(input, nameof(input.FullName)), "Họ và tên phải có ít nhất 2 ký tự và không chứa số hoặc ký tự đặc biệt.");
            }

            if (input.DateOfBirth != null)
            {
                var today = GetVietnamToday();
                var minDateOfBirth = new DateOnly(1900, 1, 1);

                if (input.DateOfBirth > today)
                {
                    ModelState.AddModelError(GetFieldName(input, nameof(input.DateOfBirth)), "Ngày sinh không được lớn hơn ngày hiện tại.");
                }

                if (input.DateOfBirth < minDateOfBirth)
                {
                    ModelState.AddModelError(GetFieldName(input, nameof(input.DateOfBirth)), "Ngày sinh không được trước 01/01/1900.");
                }

                if (input.DateOfBirth > today.AddYears(-18))
                {
                    ModelState.AddModelError(GetFieldName(input, nameof(input.DateOfBirth)), "Giáo viên phải đủ ít nhất 18 tuổi.");
                }
            }

            if (!string.IsNullOrWhiteSpace(input.Gender) &&
                input.Gender is not ("Nam" or "Nữ"))
            {
                ModelState.AddModelError(GetFieldName(input, nameof(input.Gender)), "Giới tính không hợp lệ.");
            }

            if (!string.IsNullOrWhiteSpace(input.Address) && !IsValidAddress(input.Address))
            {
                ModelState.AddModelError(GetFieldName(input, nameof(input.Address)), "Địa chỉ không hợp lệ hoặc quá ngắn.");
            }

            if (input.AvatarFile != null)
            {
                ValidateAvatarFile(input.AvatarFile, GetFieldName(input, nameof(input.AvatarFile)));
            }

            var normalizedPhone = NormalizePhoneNumber(input.PhoneNumber);

            if (!IsValidVietnamPhoneNumber(normalizedPhone))
            {
                ModelState.AddModelError(GetFieldName(input, nameof(input.PhoneNumber)), "Số điện thoại không hợp lệ.");
            }

            var teacherCodeExists = await _context.Teachers
                .AsNoTracking()
                .AnyAsync(
                    t => t.TeacherCode == input.TeacherCode &&
                        (excludedTeacherId == null || t.TeacherId != excludedTeacherId.Value),
                    cancellationToken);

            if (teacherCodeExists)
            {
                ModelState.AddModelError(GetFieldName(input, nameof(input.TeacherCode)), "Mã giáo viên đã tồn tại.");
            }

            var phoneExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(
                    u => u.NormalizedPhoneNumber == normalizedPhone &&
                        (excludedUserId == null || u.UserId != excludedUserId.Value),
                    cancellationToken);

            if (phoneExists)
            {
                ModelState.AddModelError(GetFieldName(input, nameof(input.PhoneNumber)), "Số điện thoại đã tồn tại.");
            }

            if (!string.IsNullOrWhiteSpace(input.Email))
            {
                var emailExists = await _context.Users
                    .AsNoTracking()
                    .AnyAsync(
                        u => u.Email == input.Email &&
                            (excludedUserId == null || u.UserId != excludedUserId.Value),
                        cancellationToken);

                if (emailExists)
                {
                    ModelState.AddModelError(GetFieldName(input, nameof(input.Email)), "Email đã tồn tại.");
                }
            }
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(value, out var userId)
                ? userId
                : null;
        }

        private async Task<int?> GetOwnerCenterIdAsync(
            int ownerUserId,
            CancellationToken cancellationToken)
        {
            return await _context.Centers
                .AsNoTracking()
                .Where(c => c.OwnerUserId == ownerUserId && c.Status == "Active")
                .Select(c => (int?)c.CenterId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static bool IsValidTeacherCode(string teacherCode)
        {
            return !string.IsNullOrWhiteSpace(teacherCode) &&
                teacherCode.Length <= 30 &&
                teacherCode.All(c => char.IsLetterOrDigit(c) || c == '-');
        }

        private static string NormalizePhoneNumber(string phoneNumber)
        {
            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

            return digits.StartsWith("84", StringComparison.Ordinal) && digits.Length > 9
                ? $"0{digits[2..]}"
                : digits;
        }

        private static bool IsValidVietnamPhoneNumber(string normalizedPhone)
        {
            return normalizedPhone.Length >= 10 &&
                normalizedPhone.Length <= 12 &&
                normalizedPhone.StartsWith('0') &&
                normalizedPhone.All(char.IsDigit);
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static string? NormalizeFilterValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return Regex.Replace(value.Trim(), @"\s+", " ");
        }

        private static bool IsValidFullName(string fullName)
        {
            return fullName.Length >= 2 &&
                Regex.IsMatch(fullName, @"^[\p{L}\s'.-]+$") &&
                fullName.Any(char.IsLetter) &&
                !fullName.Any(char.IsDigit);
        }

        private static bool IsValidAddress(string address)
        {
            return address.Length >= 5 &&
                !address.Any(char.IsControl);
        }

        private static string GetFieldName(
            TeacherInputBase input,
            string propertyName)
        {
            return input is TeacherEditInput
                ? $"{nameof(EditInput)}.{propertyName}"
                : $"{nameof(Input)}.{propertyName}";
        }

        private static DateOnly GetVietnamToday()
        {
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

            return DateOnly.FromDateTime(vietnamNow);
        }

        private static string GenerateNumericPassword(int length)
        {
            Span<byte> bytes = stackalloc byte[length];
            RandomNumberGenerator.Fill(bytes);

            var chars = new char[length];

            for (var i = 0; i < length; i++)
            {
                chars[i] = (char)('0' + bytes[i] % 10);
            }

            return new string(chars);
        }

        private void ValidateAvatarFile(
            IFormFile avatarFile,
            string fieldName)
        {
            const long maxFileSize = 2 * 1024 * 1024;
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            if (avatarFile.Length == 0)
            {
                ModelState.AddModelError(fieldName, "Ảnh đại diện không hợp lệ.");
                return;
            }

            if (avatarFile.Length > maxFileSize)
            {
                ModelState.AddModelError(fieldName, "Ảnh đại diện tối đa 2MB.");
                return;
            }

            var extension = Path.GetExtension(avatarFile.FileName);
            var allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(fieldName, "Ảnh đại diện chỉ nhận JPG, PNG hoặc WEBP.");
                return;
            }

            if (!allowedContentTypes.Contains(avatarFile.ContentType))
            {
                ModelState.AddModelError(fieldName, "Định dạng ảnh đại diện không hợp lệ.");
                return;
            }

            if (!HasValidImageSignature(avatarFile, extension))
            {
                ModelState.AddModelError(fieldName, "Nội dung file ảnh không hợp lệ.");
            }
        }

        private static bool HasValidImageSignature(IFormFile file, string extension)
        {
            Span<byte> header = stackalloc byte[12];

            using var stream = file.OpenReadStream();
            var bytesRead = stream.Read(header);

            if (bytesRead < 4)
            {
                return false;
            }

            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => bytesRead >= 8 &&
                    header[0] == 0x89 &&
                    header[1] == 0x50 &&
                    header[2] == 0x4E &&
                    header[3] == 0x47 &&
                    header[4] == 0x0D &&
                    header[5] == 0x0A &&
                    header[6] == 0x1A &&
                    header[7] == 0x0A,
                ".webp" => bytesRead >= 12 &&
                    header[0] == 0x52 &&
                    header[1] == 0x49 &&
                    header[2] == 0x46 &&
                    header[3] == 0x46 &&
                    header[8] == 0x57 &&
                    header[9] == 0x45 &&
                    header[10] == 0x42 &&
                    header[11] == 0x50,
                _ => false
            };
        }

        private async Task<string?> SaveAvatarAsync(
            IFormFile? avatarFile,
            string teacherCode,
            CancellationToken cancellationToken)
        {
            if (avatarFile == null || avatarFile.Length == 0)
            {
                return null;
            }

            var uploadDirectory = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "teachers");

            Directory.CreateDirectory(uploadDirectory);

            var extension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
            var fileName = $"{teacherCode.ToLowerInvariant()}-{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadDirectory, fileName);

            await using var stream = System.IO.File.Create(fullPath);
            await avatarFile.CopyToAsync(stream, cancellationToken);

            return $"/uploads/teachers/{fileName}";
        }

        private static void DeleteUploadedAvatar(string? avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl))
            {
                return;
            }

            var relativePath = avatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }

    public abstract class TeacherInputBase
    {
        [Required(ErrorMessage = "Vui lòng nhập mã giáo viên.")]
        [MaxLength(30, ErrorMessage = "Mã giáo viên tối đa 30 ký tự.")]
        public string TeacherCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [MaxLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự.")]
        public string FullName { get; set; } = string.Empty;

        public DateOnly? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [MaxLength(150, ErrorMessage = "Email tối đa 150 ký tự.")]
        public string? Email { get; set; }

        [MaxLength(10, ErrorMessage = "Giới tính tối đa 10 ký tự.")]
        public string? Gender { get; set; } = "Nam";

        [MaxLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
        public string? Address { get; set; }

        public IFormFile? AvatarFile { get; set; }

        public void Normalize()
        {
            TeacherCode = TeacherCode.Trim().ToUpperInvariant();
            FullName = NormalizeSpaces(FullName);
            PhoneNumber = PhoneNumber.Trim();
            Email = string.IsNullOrWhiteSpace(Email)
                ? null
                : Email.Trim().ToLowerInvariant();
            Gender = NormalizeOptional(Gender) ?? "Nam";
            Address = NormalizeOptional(Address);
        }

        private static string? NormalizeOptional(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = NormalizeSpaces(value);

            return string.IsNullOrWhiteSpace(normalized)
                ? null
                : normalized;
        }

        private static string NormalizeSpaces(string value)
        {
            return Regex.Replace(value.Trim(), @"\s+", " ");
        }
    }

    public sealed class TeacherInput : TeacherInputBase
    {
    }

    public sealed class TeacherEditInput : TeacherInputBase
    {
        public int TeacherId { get; set; }

        public bool RemoveAvatar { get; set; }
    }

    public sealed class TeacherListItem
    {
        public int TeacherId { get; set; }

        public string TeacherCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public int ClassCount { get; set; }

        public int StudentCount { get; set; }

        public string Status { get; set; } = string.Empty;

        public string UserStatus { get; set; } = string.Empty;

        public bool IsActive =>
            Status.Equals("Active", StringComparison.OrdinalIgnoreCase) &&
            UserStatus.Equals("Active", StringComparison.OrdinalIgnoreCase);

        public string DisplayPhoneNumber => string.IsNullOrWhiteSpace(PhoneNumber)
            ? "-"
            : PhoneNumber;

        public string DisplayEmail => string.IsNullOrWhiteSpace(Email)
            ? "-"
            : Email;

        public string Initials
        {
            get
            {
                var parts = FullName
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (parts.Length == 0)
                {
                    return "GV";
                }

                if (parts.Length == 1)
                {
                    return parts[0].Length >= 2
                        ? parts[0][..2].ToUpperInvariant()
                        : parts[0].ToUpperInvariant();
                }

                return $"{parts[^2][0]}{parts[^1][0]}".ToUpperInvariant();
            }
        }

        public string StatusText => IsActive
            ? "Đang dạy"
            : "Tạm dừng";
    }
}
