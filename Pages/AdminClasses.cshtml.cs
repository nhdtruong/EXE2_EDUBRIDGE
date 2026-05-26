using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
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
    public class AdminClassesModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminClassesModel> _logger;

        public AdminClassesModel(
            AppDbContext context,
            ILogger<AdminClassesModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        [MaxLength(150)]
        public string? Search { get; set; }

        [BindProperty]
        public ClassInput Input { get; set; } = new();

        public List<ClassListItem> Classes { get; private set; } = new();

        public List<CourseOption> CourseOptions { get; private set; } = new();

        public List<TeacherOption> TeacherOptions { get; private set; } = new();

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
                    "Owner user {OwnerUserId} has no active center for class management.",
                    ownerUserId.Value);

                Classes = new();
                return Page();
            }

            await LoadClassOptionsAsync(centerId.Value, cancellationToken);
            var query = _context.Classes
                .AsNoTracking()
                .Where(c => c.CenterId == centerId.Value);

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var keyword = Search.Trim();

                query = query.Where(c =>
                    c.ClassCode.Contains(keyword) ||
                    c.ClassName.Contains(keyword) ||
                    c.Course.CourseName.Contains(keyword) ||
                    c.Teacher.User.FullName.Contains(keyword));
            }

            Classes = await query
                .OrderBy(c => c.Status == "Active" ? 0 : 1)
                .ThenByDescending(c => c.StartDate)
                .ThenByDescending(c => c.ClassId)
                .Select(c => new ClassListItem
                {
                    ClassId = c.ClassId,
                    ClassCode = c.ClassCode,
                    ClassName = c.ClassName,
                    CourseName = c.Course.CourseName,
                    TeacherName = c.Teacher.User.FullName,
                    TotalStudents = c.Enrollments.Count(e => e.Status == "Đang học"),
                    ScheduleText = c.ScheduleText ?? string.Empty,
                    Room = c.Room ?? string.Empty,
                    Status = c.Status
                })
                .Take(100)
                .ToListAsync(cancellationToken);

            return Page();
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

            await ValidateClassInputAsync(centerId.Value, cancellationToken);

            if (!ModelState.IsValid)
            {
                await LoadPageDataAsync(centerId.Value, cancellationToken);
                return Page();
            }

            var schedules = ParseScheduleText(Input.ScheduleText!);

            var classEntity = new Class
            {
                CenterId = centerId.Value,
                CourseId = Input.CourseId,
                TeacherId = Input.TeacherId,
                ClassCode = string.Empty,
                ClassName = Input.ClassName.Trim(),
                Room = NormalizeNullable(Input.Room),
                ScheduleText = NormalizeNullable(Input.ScheduleText),
                StartDate = Input.StartDate!.Value,
                EndDate = Input.EndDate!.Value,
                Status = "Active"
            };

            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

                classEntity.ClassCode = await GenerateClassCodeAsync(
                    centerId.Value,
                    cancellationToken);

                foreach (var schedule in schedules)
                {
                    classEntity.ClassSchedules.Add(new ClassSchedule
                    {
                        DayOfWeek = schedule.DayOfWeek,
                        StartTime = schedule.StartTime,
                        EndTime = schedule.EndTime
                    });
                }

                _context.Classes.Add(classEntity);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Could not create class {ClassName}.", Input.ClassName);
                ModelState.AddModelError(string.Empty, "Không thể tạo lớp học. Vui lòng kiểm tra lại dữ liệu.");
                await LoadPageDataAsync(centerId.Value, cancellationToken);
                return Page();
            }

            TempData["ToastMessage"] = "Thêm lớp học thành công.";
            return RedirectToPage("/AdminClasses");
        }

        private async Task LoadPageDataAsync(
            int? centerId,
            CancellationToken cancellationToken)
        {
            if (centerId == null)
            {
                Classes = new();
                CourseOptions = new();
                TeacherOptions = new();
                return;
            }

            await LoadClassOptionsAsync(centerId.Value, cancellationToken);

            Classes = await _context.Classes
                .AsNoTracking()
                .Where(c => c.CenterId == centerId.Value)
                .OrderBy(c => c.Status == "Active" ? 0 : 1)
                .ThenByDescending(c => c.StartDate)
                .ThenByDescending(c => c.ClassId)
                .Select(c => new ClassListItem
                {
                    ClassId = c.ClassId,
                    ClassCode = c.ClassCode,
                    ClassName = c.ClassName,
                    CourseName = c.Course.CourseName,
                    TeacherName = c.Teacher.User.FullName,
                    TotalStudents = c.Enrollments.Count(e => e.Status == "Đang học"),
                    ScheduleText = c.ScheduleText ?? string.Empty,
                    Room = c.Room ?? string.Empty,
                    Status = c.Status
                })
                .Take(100)
                .ToListAsync(cancellationToken);
        }

        private async Task LoadClassOptionsAsync(
            int centerId,
            CancellationToken cancellationToken)
        {
            CourseOptions = await _context.Courses
                .AsNoTracking()
                .Where(c => c.CenterId == centerId && c.Status == "Active")
                .OrderBy(c => c.CourseName)
                .Select(c => new CourseOption
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName
                })
                .ToListAsync(cancellationToken);

            TeacherOptions = await _context.Teachers
                .AsNoTracking()
                .Where(t => t.CenterId == centerId && t.Status == "Active")
                .OrderBy(t => t.User.FullName)
                .Select(t => new TeacherOption
                {
                    TeacherId = t.TeacherId,
                    TeacherName = t.User.FullName
                })
                .ToListAsync(cancellationToken);
        }

        private async Task ValidateClassInputAsync(
            int centerId,
            CancellationToken cancellationToken)
        {
            Input.ClassName = Input.ClassName.Trim();

            if (Input.StartDate == null)
            {
                ModelState.AddModelError("Input.StartDate", "Vui lòng chọn ngày bắt đầu.");
            }

            if (Input.EndDate == null)
            {
                ModelState.AddModelError("Input.EndDate", "Vui lòng chọn ngày kết thúc.");
            }

            if (Input.StartDate != null && Input.EndDate != null && Input.StartDate > Input.EndDate)
            {
                ModelState.AddModelError("Input.EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
            }

            if (string.IsNullOrWhiteSpace(Input.ScheduleText))
            {
                ModelState.AddModelError("Input.ScheduleText", "Vui lòng chọn ít nhất một ngày học.");
            }
            else if (!IsValidScheduleText(Input.ScheduleText))
            {
                ModelState.AddModelError("Input.ScheduleText", "Ngày học hoặc khung giờ không hợp lệ.");
            }
            else if (HasDuplicateSchedule(Input.ScheduleText))
            {
                ModelState.AddModelError("Input.ScheduleText", "Ngày học bị trùng khung giờ.");
            }

            var courseExists = await _context.Courses
                .AsNoTracking()
                .AnyAsync(
                    c =>
                        c.CourseId == Input.CourseId &&
                        c.CenterId == centerId &&
                        c.Status == "Active",
                    cancellationToken);

            if (!courseExists)
            {
                ModelState.AddModelError("Input.CourseId", "Khóa học không hợp lệ.");
            }

            var teacherExists = await _context.Teachers
                .AsNoTracking()
                .AnyAsync(
                    t =>
                        t.TeacherId == Input.TeacherId &&
                        t.CenterId == centerId &&
                        t.Status == "Active",
                    cancellationToken);

            if (!teacherExists)
            {
                ModelState.AddModelError("Input.TeacherId", "Giáo viên không hợp lệ.");
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

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static bool IsValidScheduleText(string scheduleText)
        {
            var allowedDays = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Thứ 2",
                "Thứ 3",
                "Thứ 4",
                "Thứ 5",
                "Thứ 6",
                "Thứ 7",
                "Chủ nhật"
            };

            var items = scheduleText
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (items.Length == 0)
            {
                return false;
            }

            foreach (var item in items)
            {
                var match = Regex.Match(
                    item,
                    @"^(?<day>Thứ [2-7]|Chủ nhật) - (?<start>[0-2]\d:[0-5]\d) - (?<end>[0-2]\d:[0-5]\d)$");

                if (!match.Success)
                {
                    return false;
                }

                var day = match.Groups["day"].Value;

                if (!allowedDays.Contains(day))
                {
                    return false;
                }

                if (!TimeOnly.TryParse(match.Groups["start"].Value, out var startTime) ||
                    !TimeOnly.TryParse(match.Groups["end"].Value, out var endTime))
                {
                    return false;
                }

                if (endTime <= startTime)
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<string> GenerateClassCodeAsync(
            int centerId,
            CancellationToken cancellationToken)
        {
            var yearMonth = DateTime.UtcNow.ToString("yyyyMM");
            var counter = await _context.ClassCodeCounters
                .FirstOrDefaultAsync(
                    c => c.CenterId == centerId && c.YearMonth == yearMonth,
                    cancellationToken);

            if (counter == null)
            {
                var prefix = $"CLS-{centerId}-{yearMonth}-";
                var lastExistingCode = await _context.Classes
                    .AsNoTracking()
                    .Where(c =>
                        c.CenterId == centerId &&
                        c.ClassCode.StartsWith(prefix))
                    .OrderByDescending(c => c.ClassCode)
                    .Select(c => c.ClassCode)
                    .FirstOrDefaultAsync(cancellationToken);

                counter = new ClassCodeCounter
                {
                    CenterId = centerId,
                    YearMonth = yearMonth,
                    LastNumber = GetClassCodeSequence(lastExistingCode)
                };

                _context.ClassCodeCounters.Add(counter);
            }

            counter.LastNumber += 1;

            return $"CLS-{centerId}-{yearMonth}-{counter.LastNumber:0000}";
        }

        private static int GetClassCodeSequence(string? classCode)
        {
            if (string.IsNullOrWhiteSpace(classCode))
            {
                return 0;
            }

            var lastPart = classCode.Split('-').LastOrDefault();

            return int.TryParse(lastPart, out var sequence)
                ? sequence
                : 0;
        }

        private static bool HasDuplicateSchedule(string scheduleText)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var schedule in ParseScheduleText(scheduleText))
            {
                var key = $"{schedule.DayOfWeek}:{schedule.StartTime:HH:mm}:{schedule.EndTime:HH:mm}";

                if (!keys.Add(key))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<ParsedClassSchedule> ParseScheduleText(string scheduleText)
        {
            return scheduleText
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(item =>
                {
                    var match = Regex.Match(
                        item,
                        @"^(?<day>Thứ [2-7]|Chủ nhật) - (?<start>[0-2]\d:[0-5]\d) - (?<end>[0-2]\d:[0-5]\d)$");

                    return new ParsedClassSchedule
                    {
                        DayOfWeek = MapDayOfWeek(match.Groups["day"].Value),
                        StartTime = TimeOnly.Parse(match.Groups["start"].Value),
                        EndTime = TimeOnly.Parse(match.Groups["end"].Value)
                    };
                })
                .ToList();
        }

        private static byte MapDayOfWeek(string day)
        {
            return day switch
            {
                "Thứ 2" => 1,
                "Thứ 3" => 2,
                "Thứ 4" => 3,
                "Thứ 5" => 4,
                "Thứ 6" => 5,
                "Thứ 7" => 6,
                "Chủ nhật" => 7,
                _ => 0
            };
        }
    }

    public sealed class ParsedClassSchedule
    {
        public byte DayOfWeek { get; set; }

        public TimeOnly StartTime { get; set; }

        public TimeOnly EndTime { get; set; }
    }

    public sealed class ClassInput
    {
        [Required(ErrorMessage = "Vui lòng nhập tên lớp.")]
        [MaxLength(150, ErrorMessage = "Tên lớp tối đa 150 ký tự.")]
        public string ClassName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn khóa học.")]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giáo viên.")]
        public int TeacherId { get; set; }

        [MaxLength(50, ErrorMessage = "Phòng học tối đa 50 ký tự.")]
        public string? Room { get; set; }

        [MaxLength(255, ErrorMessage = "Lịch học tối đa 255 ký tự.")]
        public string? ScheduleText { get; set; }

        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }
    }

    public sealed class CourseOption
    {
        public int CourseId { get; set; }

        public string CourseName { get; set; } = string.Empty;
    }

    public sealed class TeacherOption
    {
        public int TeacherId { get; set; }

        public string TeacherName { get; set; } = string.Empty;
    }

    public sealed class ClassListItem
    {
        public int ClassId { get; set; }

        public string ClassCode { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public string CourseName { get; set; } = string.Empty;

        public string TeacherName { get; set; } = string.Empty;

        public int TotalStudents { get; set; }

        public string ScheduleText { get; set; } = string.Empty;

        public IReadOnlyList<string> ScheduleLines => string.IsNullOrWhiteSpace(ScheduleText)
            ? new[] { "-" }
            : ScheduleText
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        public string Room { get; set; } = string.Empty;

        public string DisplayRoom => string.IsNullOrWhiteSpace(Room)
            ? "-"
            : Room;

        public string Status { get; set; } = string.Empty;

        public string DisplaySchedule
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Room))
                {
                    return ScheduleText;
                }

                if (string.IsNullOrWhiteSpace(ScheduleText))
                {
                    return Room;
                }

                return $"{ScheduleText} - {Room}";
            }
        }

        public string StatusText => Status.ToUpperInvariant() switch
        {
            "ACTIVE" => "Đang hoạt động",
            "INACTIVE" => "Tạm dừng",
            "CLOSED" => "Đã đóng",
            _ => "Không xác định"
        };

        public string StatusBadgeClass => Status.ToUpperInvariant() switch
        {
            "ACTIVE" => "bg-green-100 text-green-700",
            "INACTIVE" => "bg-yellow-100 text-yellow-700",
            "CLOSED" => "bg-gray-200 text-gray-700",
            _ => "bg-red-100 text-red-700"
        };
    }
}
