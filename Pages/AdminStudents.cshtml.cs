using System.Security.Claims;
using System.Text.RegularExpressions;
using EduBridge.Models;
using EduBridge.Services.Classes;
using EduBridge.Services.Settings;
using EduBridge.Services.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminStudentsModel : PageModel
    {
        private readonly ICenterSettingsService _centerSettingsService;
        private readonly IClassManagementService _classManagementService;
        private readonly IStudentManagementService _studentService;
        private readonly ILogger<AdminStudentsModel> _logger;

        public AdminStudentsModel(
            ICenterSettingsService centerSettingsService,
            IClassManagementService classManagementService,
            IStudentManagementService studentService,
            ILogger<AdminStudentsModel> logger)
        {
            _centerSettingsService = centerSettingsService;
            _classManagementService = classManagementService;
            _studentService = studentService;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string? StudentSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ParentSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ContactSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? GenderFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ClassFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 20;

        public List<StudentListItem> Students { get; private set; } = new();

        public List<StudentClassFilterItem> ClassFilterOptions { get; private set; } = new();

        public int[] PageSizeOptions { get; } = { 10, 20, 50, 100, 200, 500 };

        public int TotalStudents { get; private set; }

        public int TotalPages => TotalStudents == 0
            ? 1
            : (int)Math.Ceiling(TotalStudents / (double)PageSize);

        public int FirstItemNumber => TotalStudents == 0
            ? 0
            : (PageNumber - 1) * PageSize + 1;

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
                    "Owner user {OwnerUserId} has no active center for student management.",
                    ownerUserId.Value);

                Students = new();
                return Page();
            }

            NormalizeFilters();

            if (!ValidateFilters())
            {
                await LoadClassFilterOptionsAsync(centerId.Value, cancellationToken);
                Students = new();
                return Page();
            }

            await LoadStudentsAsync(centerId.Value, cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(
            int studentId,
            CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();
            if (ownerUserId == null) return RedirectToPage("/Login");

            var result = await _studentService.ToggleStudentStatusAsync(ownerUserId.Value, studentId, cancellationToken);

            if (!result.IsSuccess)
            {
                if (result.Message.Contains("Không tìm thấy học sinh")) return NotFound();

                TempData["ToastType"] = "error";
                TempData["ToastTitle"] = "Thất bại";
                TempData["ToastMessage"] = result.Message;
                return RedirectToPage("/AdminStudents", BuildRouteValues());
            }

            TempData["ToastType"] = "success";
            TempData["ToastTitle"] = "Thành công";
            TempData["ToastMessage"] = result.Value?.Status == "Active"
                ? "Đã kích hoạt hồ sơ học sinh."
                : "Đã ngừng hoạt động hồ sơ học sinh.";

            return RedirectToPage("/AdminStudents", BuildRouteValues());
        }

        public async Task<IActionResult> OnPostDeleteAsync(
            int studentId,
            CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();
            if (ownerUserId == null) return RedirectToPage("/Login");

            var result = await _studentService.DeleteStudentAsync(ownerUserId.Value, studentId, cancellationToken);

            if (!result.IsSuccess)
            {
                if (result.Message.Contains("Không tìm thấy học sinh")) return NotFound();

                TempData["ToastType"] = "error";
                TempData["ToastTitle"] = "Thất bại";
                TempData["ToastMessage"] = result.Message;
                return RedirectToPage("/AdminStudents", BuildRouteValues());
            }

            TempData["ToastType"] = "success";
            TempData["ToastTitle"] = "Thành công";
            TempData["ToastMessage"] = "Đã xóa học sinh.";

            return RedirectToPage("/AdminStudents", BuildRouteValues());
        }

        private async Task LoadStudentsAsync(
            int centerId,
            CancellationToken cancellationToken)
        {
            await LoadClassFilterOptionsAsync(centerId, cancellationToken);

            var ownerUserId = GetCurrentUserId();
            if (ownerUserId == null) return;

            var query = new EduBridge.Contracts.Students.StudentQuery
            {
                Keyword = StudentSearch,
                ParentKeyword = ParentSearch,
                ContactKeyword = ContactSearch,
                Gender = GenderFilter,
                ClassId = ClassFilter,
                Status = StatusFilter,
                PageNumber = PageNumber,
                PageSize = PageSize
            };

            var result = await _studentService.GetStudentsAsync(ownerUserId.Value, query, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                Students = new();
                TotalStudents = 0;
                return;
            }

            TotalStudents = result.Value.TotalItems;
            PageNumber = result.Value.Page;

            Students = result.Value.Data.Select(s => new StudentListItem
            {
                StudentId = s.StudentId,
                StudentCode = s.StudentCode,
                StudentName = s.FullName,
                Gender = s.Gender,
                StudentPhone = s.PhoneNumber,
                StudentEmail = s.Email,
                DateOfBirth = s.DateOfBirth,
                StudentStatus = s.Status,
                ParentUserId = s.ParentUserId,
                ParentName = s.ParentName,
                ParentPhone = s.ParentPhone,
                ParentEmail = s.ParentEmail,
                CurrentClasses = s.CurrentClasses.Select(c => new StudentClassItem
                {
                    ClassId = c.ClassId,
                    ClassCode = c.ClassCode,
                    ClassName = c.ClassName,
                    CourseName = c.CourseName
                }).ToList()
            }).ToList();
        }

        private async Task LoadClassFilterOptionsAsync(
            int centerId,
            CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();
            if (ownerUserId == null) return;

            var classOptions = await _classManagementService.GetClassOptionsAsync(ownerUserId.Value, cancellationToken);
            if (classOptions.IsSuccess && classOptions.Value != null)
            {
                // ClassOptionDto in GetClassOptionsAsync might not be Class but we can use GetClassesAsync or just filter if needed. Wait, does ClassDropdownOptionsResponse have what we need?
                // Let's check ClassDropdownOptionsResponse. Wait, GetClassOptionsAsync doesn't return classes, it returns Courses, Teachers, Rooms!
                // Ah, GetClassOptionsAsync returns Dropdown options for *creating* a class. It doesn't return the list of Active Classes.
                // We should use GetClassesAsync from IClassManagementService or add a specific method.
                // Let's use GetClassesAsync.
                var query = new EduBridge.Contracts.Classes.ClassQuery(null, "Active", null, null, 1, 1000);
                var result = await _classManagementService.GetClassesAsync(ownerUserId.Value, query, cancellationToken);
                if (result.IsSuccess && result.Value != null)
                {
                    ClassFilterOptions = result.Value.Items
                        .OrderBy(c => c.ClassName)
                        .Select(c => new StudentClassFilterItem
                        {
                            ClassId = c.ClassId,
                            ClassName = c.ClassName,
                            ClassCode = c.ClassCode
                        }).ToList();
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
            return await _centerSettingsService.GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        }

        private void NormalizeFilters()
        {
            StudentSearch = NormalizeFilterValue(StudentSearch);
            ParentSearch = NormalizeFilterValue(ParentSearch);
            ContactSearch = NormalizeFilterValue(ContactSearch);
            GenderFilter = NormalizeFilterValue(GenderFilter);
            StatusFilter = NormalizeFilterValue(StatusFilter);

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

            ValidateTextFilter(StudentSearch, "StudentSearch", "Từ khóa mã/tên học sinh", ref valid);
            ValidateTextFilter(ParentSearch, "ParentSearch", "Từ khóa phụ huynh", ref valid);
            ValidateTextFilter(ContactSearch, "ContactSearch", "Từ khóa SĐT/email", ref valid);

            if (!string.IsNullOrWhiteSpace(GenderFilter) &&
                GenderFilter is not ("Nam" or "Nữ"))
            {
                ModelState.AddModelError("GenderFilter", "Giới tính lọc không hợp lệ.");
                valid = false;
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter) &&
                StatusFilter is not ("Active" or "Inactive"))
            {
                ModelState.AddModelError("StatusFilter", "Trạng thái lọc không hợp lệ.");
                valid = false;
            }

            if (ClassFilter is <= 0)
            {
                ModelState.AddModelError("ClassFilter", "Lớp lọc không hợp lệ.");
                valid = false;
            }

            return valid;
        }

        private void ValidateTextFilter(
            string? value,
            string fieldName,
            string displayName,
            ref bool valid)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (value.Length > 150)
            {
                ModelState.AddModelError(fieldName, $"{displayName} tối đa 150 ký tự.");
                valid = false;
            }

            if (value.Any(char.IsControl))
            {
                ModelState.AddModelError(fieldName, $"{displayName} không hợp lệ.");
                valid = false;
            }
        }

        private object BuildRouteValues(bool resetPage = false)
        {
            return new
            {
                StudentSearch,
                ParentSearch,
                ContactSearch,
                GenderFilter,
                ClassFilter,
                StatusFilter,
                PageNumber = resetPage ? 1 : PageNumber,
                PageSize
            };
        }

        private static string? NormalizeFilterValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return Regex.Replace(value.Trim(), @"\s+", " ");
        }

        private static string NormalizePhoneNumber(string value)
        {
            var digits = new string(value.Where(char.IsDigit).ToArray());

            return digits.StartsWith("84", StringComparison.Ordinal) && digits.Length > 9
                ? $"0{digits[2..]}"
                : digits;
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

    public sealed class StudentListItem
    {
        public int StudentId { get; set; }

        public string StudentCode { get; set; } = string.Empty;

        public string StudentName { get; set; } = string.Empty;

        public string? Gender { get; set; }

        public string? StudentPhone { get; set; }

        public string? StudentEmail { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        public List<StudentClassItem> CurrentClasses { get; set; } = new();

        public string ParentName { get; set; } = string.Empty;

        public int ParentUserId { get; set; }

        public string? ParentPhone { get; set; }

        public string? ParentEmail { get; set; }

        public string StudentStatus { get; set; } = string.Empty;

        public string DisplayParentPhone => string.IsNullOrWhiteSpace(ParentPhone)
            ? "-"
            : ParentPhone;

        public string DisplayParentEmail => string.IsNullOrWhiteSpace(ParentEmail)
            ? "-"
            : ParentEmail;

        public string DisplayStudentPhone => string.IsNullOrWhiteSpace(StudentPhone)
            ? "-"
            : StudentPhone;

        public string DisplayStudentEmail => string.IsNullOrWhiteSpace(StudentEmail)
            ? "-"
            : StudentEmail;

        public string DisplayDateOfBirth => DateOfBirth?.ToString("dd/MM/yyyy") ?? "-";

        public string DisplayGender => string.IsNullOrWhiteSpace(Gender)
            ? "-"
            : Gender;

        public bool IsActive => StudentStatus.Equals("Active", StringComparison.OrdinalIgnoreCase);

        public string StatusText => IsActive
            ? "Đang hoạt động"
            : "Ngừng hoạt động";

        public string Initials
        {
            get
            {
                var parts = StudentName
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (parts.Length == 0)
                {
                    return "HS";
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
    }

    public sealed class StudentClassItem
    {
        public int ClassId { get; set; }

        public string ClassCode { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public string CourseName { get; set; } = string.Empty;
    }

    public sealed class StudentClassFilterItem
    {
        public int ClassId { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public string ClassCode { get; set; } = string.Empty;

        public string DisplayName => $"{ClassName} ({ClassCode})";
    }
}
