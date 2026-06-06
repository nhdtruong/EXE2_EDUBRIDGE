using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;
using EduBridge.Data;
using EduBridge.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EduBridge.Services.Courses;
using EduBridge.Contracts.Courses;

namespace EduBridge.Pages
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminCoursesModel : PageModel
    {
        private readonly ICourseManagementService _service;
        private readonly ILogger<AdminCoursesModel> _logger;

        public AdminCoursesModel(ICourseManagementService service, ILogger<AdminCoursesModel> logger)
        {
            _service = service;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        [MaxLength(150)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        [MaxLength(20)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 20;

        public List<CourseListItem> Courses { get; private set; } = new();

        public int[] PageSizeOptions { get; } = { 10, 20, 50, 100, 200, 500 };

        public int TotalCourses { get; private set; }

        public int TotalPages => TotalCourses == 0
            ? 1
            : (int)Math.Ceiling(TotalCourses / (double)PageSize);

        public int FirstItemNumber => TotalCourses == 0
            ? 0
            : (PageNumber - 1) * PageSize + 1;

        [BindProperty]
        public CourseInput Input { get; set; } = new();

        [BindProperty]
        public CourseEditInput EditInput { get; set; } = new();

        public bool IsCreateModalOpen { get; private set; }

        public bool IsEditModalOpen { get; private set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();

            if (ownerUserId == null)
            {
                return RedirectToPage("/Login");
            }

            NormalizeFilters();

            if (!ValidateFilters())
            {
                Courses = new();
                return Page();
            }

            await LoadCoursesAsync(ownerUserId.Value, cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnGetEditAsync(int courseId, CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();

            if (ownerUserId == null)
            {
                return RedirectToPage("/Login");
            }

            NormalizeFilters();

            if (!ValidateFilters())
            {
                Courses = new();
                return Page();
            }

            var result = await _service.GetCourseAsync(ownerUserId.Value, courseId, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                SetToast("Thất bại", result.Message ?? "Không tìm thấy môn học cần xem/sửa.", "error");
                return RedirectToPage("/AdminCourses", BuildRouteValues());
            }

            var course = result.Value;
            EditInput = new CourseEditInput
            {
                CourseId = course.CourseId,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                Description = course.Description,
                TotalSessions = course.TotalSessions,
                TuitionFee = course.TuitionFee,
                Status = course.Status
            };
            
            IsEditModalOpen = true;
            await LoadCoursesAsync(ownerUserId.Value, cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();

            if (ownerUserId == null)
            {
                return RedirectToPage("/Login");
            }

            NormalizeFilters();
            NormalizeInput();

            if (!ValidateInput())
            {
                IsCreateModalOpen = true;
                await LoadCoursesAsync(ownerUserId.Value, cancellationToken);
                return Page();
            }

            var request = new SaveCourseRequest
            {
                CourseCode = Input.CourseCode,
                CourseName = Input.CourseName,
                Description = Input.Description,
                TotalSessions = Input.TotalSessions,
                TuitionFee = Input.TuitionFee,
                Status = Input.Status
            };

            var result = await _service.CreateCourseAsync(ownerUserId.Value, request, cancellationToken);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Không thể thêm môn học.");
                IsCreateModalOpen = true;
                await LoadCoursesAsync(ownerUserId.Value, cancellationToken);
                return Page();
            }

            SetToast("Thành công", "Thêm môn học thành công.", "success");
            return RedirectToPage("/AdminCourses", new { PageNumber = 1, PageSize });
        }

        public async Task<IActionResult> OnPostUpdateAsync(CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();

            if (ownerUserId == null)
            {
                return RedirectToPage("/Login");
            }

            NormalizeFilters();
            NormalizeEditInput();

            if (!ValidateEditInput())
            {
                IsEditModalOpen = true;
                await LoadCoursesAsync(ownerUserId.Value, cancellationToken);
                return Page();
            }

            var request = new SaveCourseRequest
            {
                CourseCode = EditInput.CourseCode,
                CourseName = EditInput.CourseName,
                Description = EditInput.Description,
                TotalSessions = EditInput.TotalSessions,
                TuitionFee = EditInput.TuitionFee,
                Status = EditInput.Status
            };

            var result = await _service.UpdateCourseAsync(ownerUserId.Value, EditInput.CourseId, request, cancellationToken);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Không thể cập nhật môn học.");
                IsEditModalOpen = true;
                await LoadCoursesAsync(ownerUserId.Value, cancellationToken);
                return Page();
            }

            SetToast("Thành công", "Cập nhật môn học thành công.", "success");
            return RedirectToPage("/AdminCourses", BuildRouteValues());
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int courseId, string currentStatus, CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();

            if (ownerUserId == null)
            {
                return RedirectToPage("/Login");
            }

            NormalizeFilters();

            var nextStatus = currentStatus == "Active" ? "Inactive" : "Active";
            var result = await _service.SetStatusAsync(ownerUserId.Value, courseId, nextStatus, cancellationToken);

            if (!result.IsSuccess)
            {
                SetToast("Thất bại", result.Message ?? "Không tìm thấy môn học cần đổi trạng thái.", "error");
                return RedirectToPage("/AdminCourses", BuildRouteValues());
            }

            SetToast(
                "Thành công",
                nextStatus == "Active"
                    ? "Đã bật môn học."
                    : "Đã tạm dừng môn học.",
                "success");

            return RedirectToPage("/AdminCourses", BuildRouteValues());
        }

        public async Task<IActionResult> OnPostDeleteAsync(int courseId, CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();

            if (ownerUserId == null)
            {
                return RedirectToPage("/Login");
            }

            NormalizeFilters();

            var result = await _service.DeleteCourseAsync(ownerUserId.Value, courseId, cancellationToken);

            if (!result.IsSuccess)
            {
                SetToast("Thất bại", result.Message ?? "Không tìm thấy môn học cần xóa.", "error");
                return RedirectToPage("/AdminCourses", BuildRouteValues());
            }

            SetToast("Thành công", "Xóa môn học thành công.", "success");
            return RedirectToPage("/AdminCourses", BuildRouteValues());
        }

        private async Task LoadCoursesAsync(int ownerUserId, CancellationToken cancellationToken)
        {
            var query = new CourseQuery
            {
                Keyword = Search,
                Status = StatusFilter,
                PageNumber = PageNumber,
                PageSize = PageSize
            };

            var result = await _service.GetCoursesAsync(ownerUserId, query, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                var pagedData = result.Value;
                TotalCourses = pagedData.TotalItems;

                if (PageNumber > TotalPages)
                {
                    PageNumber = TotalPages;
                    query.PageNumber = PageNumber;
                    
                    if (PageNumber > 0)
                    {
                        var adjustedResult = await _service.GetCoursesAsync(ownerUserId, query, cancellationToken);
                        if (adjustedResult.IsSuccess && adjustedResult.Value != null)
                        {
                            pagedData = adjustedResult.Value;
                        }
                    }
                }

                Courses = pagedData.Data.Select(c => new CourseListItem
                {
                    CourseId = c.CourseId,
                    CourseCode = c.CourseCode,
                    CourseName = c.CourseName,
                    TotalSessions = c.TotalSessions,
                    TuitionFee = c.TuitionFee,
                    Status = c.Status,
                    ClassCount = c.ClassCount
                }).ToList();
            }
            else
            {
                TotalCourses = 0;
                Courses = new List<CourseListItem>();
            }
        }

        private void NormalizeFilters()
        {
            Search = NormalizeFilterValue(Search);
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
                    ModelState.AddModelError("Search", "Từ khóa mã/tên môn tối đa 150 ký tự.");
                    valid = false;
                }

                if (Search.Any(char.IsControl))
                {
                    ModelState.AddModelError("Search", "Từ khóa mã/tên môn không hợp lệ.");
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

        private void NormalizeInput()
        {
            Input.CourseCode = NormalizeCourseCode(Input.CourseCode);
            Input.CourseName = NormalizeFilterValue(Input.CourseName) ?? string.Empty;
            Input.Description = NormalizeNullableText(Input.Description);
            Input.Status = NormalizeFilterValue(Input.Status) ?? "Active";
        }

        private void NormalizeEditInput()
        {
            EditInput.CourseCode = NormalizeCourseCode(EditInput.CourseCode);
            EditInput.CourseName = NormalizeFilterValue(EditInput.CourseName) ?? string.Empty;
            EditInput.Description = NormalizeNullableText(EditInput.Description);
            EditInput.Status = NormalizeFilterValue(EditInput.Status) ?? "Active";
        }

        private bool ValidateInput()
        {
            var valid = true;

            if (string.IsNullOrWhiteSpace(Input.CourseCode))
            {
                ModelState.AddModelError("Input.CourseCode", "Mã môn là bắt buộc.");
                valid = false;
            }
            else
            {
                if (Input.CourseCode.Length > 30)
                {
                    ModelState.AddModelError("Input.CourseCode", "Mã môn tối đa 30 ký tự.");
                    valid = false;
                }

                if (Input.CourseCode.Any(char.IsControl))
                {
                    ModelState.AddModelError("Input.CourseCode", "Mã môn không hợp lệ.");
                    valid = false;
                }
            }

            if (string.IsNullOrWhiteSpace(Input.CourseName))
            {
                ModelState.AddModelError("Input.CourseName", "Tên môn là bắt buộc.");
                valid = false;
            }
            else if (Input.CourseName.Length > 150)
            {
                ModelState.AddModelError("Input.CourseName", "Tên môn tối đa 150 ký tự.");
                valid = false;
            }

            if (!string.IsNullOrWhiteSpace(Input.Description) && Input.Description.Length > 500)
            {
                ModelState.AddModelError("Input.Description", "Mô tả tối đa 500 ký tự.");
                valid = false;
            }

            if (Input.TotalSessions.HasValue && Input.TotalSessions is < 1 or > 1000)
            {
                ModelState.AddModelError("Input.TotalSessions", "Tổng số buổi phải từ 1 đến 1000.");
                valid = false;
            }

            if (Input.TuitionFee.HasValue && Input.TuitionFee < 0)
            {
                ModelState.AddModelError("Input.TuitionFee", "Học phí không được âm.");
                valid = false;
            }

            if (Input.Status is not ("Active" or "Inactive"))
            {
                ModelState.AddModelError("Input.Status", "Trạng thái môn học không hợp lệ.");
                valid = false;
            }

            return valid;
        }

        private bool ValidateEditInput()
        {
            var valid = true;

            if (EditInput.CourseId <= 0)
            {
                ModelState.AddModelError("EditInput.CourseId", "Môn học cần cập nhật không hợp lệ.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(EditInput.CourseCode))
            {
                ModelState.AddModelError("EditInput.CourseCode", "Mã môn là bắt buộc.");
                valid = false;
            }
            else
            {
                if (EditInput.CourseCode.Length > 30)
                {
                    ModelState.AddModelError("EditInput.CourseCode", "Mã môn tối đa 30 ký tự.");
                    valid = false;
                }

                if (EditInput.CourseCode.Any(char.IsControl))
                {
                    ModelState.AddModelError("EditInput.CourseCode", "Mã môn không hợp lệ.");
                    valid = false;
                }
            }

            if (string.IsNullOrWhiteSpace(EditInput.CourseName))
            {
                ModelState.AddModelError("EditInput.CourseName", "Tên môn là bắt buộc.");
                valid = false;
            }
            else if (EditInput.CourseName.Length > 150)
            {
                ModelState.AddModelError("EditInput.CourseName", "Tên môn tối đa 150 ký tự.");
                valid = false;
            }

            if (!string.IsNullOrWhiteSpace(EditInput.Description) && EditInput.Description.Length > 500)
            {
                ModelState.AddModelError("EditInput.Description", "Mô tả tối đa 500 ký tự.");
                valid = false;
            }

            if (EditInput.TotalSessions.HasValue && EditInput.TotalSessions is < 1 or > 1000)
            {
                ModelState.AddModelError("EditInput.TotalSessions", "Tổng số buổi phải từ 1 đến 1000.");
                valid = false;
            }

            if (EditInput.TuitionFee.HasValue && EditInput.TuitionFee < 0)
            {
                ModelState.AddModelError("EditInput.TuitionFee", "Học phí không được âm.");
                valid = false;
            }

            if (EditInput.Status is not ("Active" or "Inactive"))
            {
                ModelState.AddModelError("EditInput.Status", "Trạng thái môn học không hợp lệ.");
                valid = false;
            }

            return valid;
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(value, out var userId)
                ? userId
                : null;
        }



        private static int? ExtractNumericId(string value)
        {
            var digits = new string(value.Where(char.IsDigit).ToArray());

            if (string.IsNullOrWhiteSpace(digits))
            {
                return null;
            }

            return int.TryParse(digits.TrimStart('0'), out var id) && id > 0
                ? id
                : null;
        }

        private static string? NormalizeFilterValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return Regex.Replace(value.Trim(), @"\s+", " ");
        }

        private static string NormalizeCourseCode(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : Regex.Replace(value.Trim(), @"\s+", " ");
        }

        private static string? NormalizeNullableText(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : Regex.Replace(value.Trim(), @"\s+", " ");
        }

        private void SetToast(string title, string message, string type)
        {
            TempData["ToastTitle"] = title;
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = type;
        }

        private object BuildRouteValues()
        {
            return new
            {
                Search,
                StatusFilter,
                PageNumber,
                PageSize
            };
        }

        public sealed class CourseListItem
        {
            public int CourseId { get; set; }

            public string CourseCode { get; set; } = string.Empty;

            public string CourseName { get; set; } = string.Empty;

            public int TotalSessions { get; set; }

            public decimal? TuitionFee { get; set; }

            public string Status { get; set; } = string.Empty;

            public int ClassCount { get; set; }

            public bool HasClasses => ClassCount > 0;

            public bool IsActive => Status == "Active";

            public string StatusText => Status switch
            {
                "Active" => "Đang sử dụng",
                "Inactive" => "Tạm dừng",
                _ => string.IsNullOrWhiteSpace(Status) ? "Chưa xác định" : Status
            };

            public string TuitionFeeText => TuitionFee.HasValue
                ? $"{TuitionFee.Value:N0} VNĐ"
                : "-";

            public string StatusBadgeClass => Status switch
            {
                "Active" => "bg-green-100 text-green-700",
                "Inactive" => "bg-gray-100 text-gray-600",
                _ => "bg-slate-100 text-slate-600"
            };
        }

        public class CourseInput
        {
            public string CourseCode { get; set; } = string.Empty;

            public string CourseName { get; set; } = string.Empty;

            public string? Description { get; set; }

            public int? TotalSessions { get; set; }

            public decimal? TuitionFee { get; set; }

            public string Status { get; set; } = "Active";
        }

        public sealed class CourseEditInput : CourseInput
        {
            public int CourseId { get; set; }
        }
    }
}
