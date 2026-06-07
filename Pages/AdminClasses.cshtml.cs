using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EduBridge.Contracts.Classes;
using EduBridge.Contracts.Rooms;
using EduBridge.Contracts.Shifts;
using EduBridge.Services.Classes;
using EduBridge.Services.Rooms;
using EduBridge.Services.Shifts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminClassesModel : PageModel
    {
        private readonly IClassManagementService _classManagementService;
        private readonly IRoomManagementService _roomManagementService;
        private readonly IShiftManagementService _shiftManagementService;
        private readonly ILogger<AdminClassesModel> _logger;

        public AdminClassesModel(
            IClassManagementService classManagementService,
            IRoomManagementService roomManagementService,
            IShiftManagementService shiftManagementService,
            ILogger<AdminClassesModel> logger)
        {
            _classManagementService = classManagementService;
            _roomManagementService = roomManagementService;
            _shiftManagementService = shiftManagementService;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        [MaxLength(150)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        [MaxLength(150)]
        public string? ClassTeacherSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ClassCourseId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ClassStatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly? ClassStartDateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly? ClassStartDateTo { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly? ClassEndDateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly? ClassEndDateTo { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ClassRoomId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int ClassPageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int ClassPageSize { get; set; } = 20;

        [BindProperty(SupportsGet = true)]
        public string? Tab { get; set; }

        [BindProperty(SupportsGet = true)]
        [MaxLength(150)]
        public string? RoomSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? RoomStatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int RoomPageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int RoomPageSize { get; set; } = 20;

        [BindProperty(SupportsGet = true)]
        [MaxLength(150)]
        public string? ShiftSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ShiftStatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int ShiftPageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int ShiftPageSize { get; set; } = 20;

        [BindProperty]
        public SaveRoomRequest CreateRoomInput { get; set; } = new("", "", null, null, "Active");

        [BindProperty]
        public RoomEditInput EditRoomInput { get; set; } = new();

        [BindProperty]
        public SaveShiftRequest CreateShiftInput { get; set; } = new("", "", default, default, "Active", null);

        [BindProperty]
        public ShiftEditInput EditShiftInput { get; set; } = new();

        public List<ClassListItemDto> Classes { get; private set; } = new();

        public List<RoomListItemDto> Rooms { get; private set; } = new();

        public List<ShiftListItemDto> StudyShifts { get; private set; } = new();

        public List<CourseOptionDto> CourseOptions { get; private set; } = new();

        public List<TeacherOptionDto> TeacherOptions { get; private set; } = new();

        public List<RoomOptionDto> RoomOptions { get; private set; } = new();

        public bool IsRoomsTab => string.Equals(Tab, "rooms", StringComparison.OrdinalIgnoreCase);

        public bool IsShiftsTab => string.Equals(Tab, "shifts", StringComparison.OrdinalIgnoreCase);

        public int[] ClassPageSizeOptions { get; } = { 10, 20, 50, 100, 200, 500 };

        public int TotalClasses { get; private set; }

        public int TotalClassPages { get; private set; }

        public int FirstClassItemNumber => TotalClasses == 0
            ? 0
            : (ClassPageNumber - 1) * ClassPageSize + 1;

        public int[] RoomPageSizeOptions { get; } = { 10, 20, 50, 100, 200, 500 };

        public int TotalRooms { get; private set; }

        public int TotalRoomPages { get; private set; }

        public int FirstRoomItemNumber => TotalRooms == 0
            ? 0
            : (RoomPageNumber - 1) * RoomPageSize + 1;

        public int[] ShiftPageSizeOptions { get; } = { 10, 20, 50, 100, 200, 500 };

        public int TotalStudyShifts { get; private set; }

        public int TotalShiftPages { get; private set; }

        public int FirstShiftItemNumber => TotalStudyShifts == 0
            ? 0
            : (ShiftPageNumber - 1) * ShiftPageSize + 1;

        public bool OpenCreateRoomModal { get; private set; }

        public bool OpenEditRoomModal { get; private set; }

        public bool OpenCreateShiftModal { get; private set; }

        public bool OpenEditShiftModal { get; private set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();

            if (ownerUserId == null)
            {
                return RedirectToPage("/Login");
            }

            NormalizeTabsAndSearch();
            await LoadPageDataAsync(ownerUserId.Value, cancellationToken);

            return Page();
        }

        public async Task<IActionResult> OnPostCloseClassAsync(int classId, CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();
            if (ownerUserId == null) return RedirectToPage("/Login");

            var result = await _classManagementService.CloseAsync(ownerUserId.Value, classId, cancellationToken);
            SetToast(result.IsSuccess ? "Thành công" : "Thất bại", result.Message, result.IsSuccess ? "success" : "error");
            return RedirectToClassesTab();
        }

        public async Task<IActionResult> OnPostDeleteClassAsync(int classId, CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();
            if (ownerUserId == null) return RedirectToPage("/Login");

            var result = await _classManagementService.SoftDeleteAsync(ownerUserId.Value, classId, cancellationToken);
            SetToast(result.IsSuccess ? "Thành công" : "Thất bại", result.Message, result.IsSuccess ? "success" : "error");
            return RedirectToClassesTab();
        }

        public async Task<IActionResult> OnPostCreateRoomAsync(CancellationToken cancellationToken)
        {
            RemoveModelStatePrefix("Input");
            RemoveModelStatePrefix(nameof(EditRoomInput));
            Tab = "rooms";
            NormalizeTabsAndSearch();

            var ownerUserId = GetCurrentUserId();
            if (ownerUserId == null) return RedirectToPage("/Login");

            if (!ModelState.IsValid)
            {
                OpenCreateRoomModal = true;
                await LoadPageDataAsync(ownerUserId.Value, cancellationToken);
                return Page();
            }

            var result = await _roomManagementService.CreateAsync(ownerUserId.Value, CreateRoomInput, cancellationToken);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                OpenCreateRoomModal = true;
                await LoadPageDataAsync(ownerUserId.Value, cancellationToken);
                return Page();
            }

            SetToast("Thành công", result.Message, "success");
            return RedirectToRoomsTab();
        }

        public async Task<IActionResult> OnPostUpdateRoomAsync(CancellationToken cancellationToken)
        {
            RemoveModelStatePrefix("Input");
            RemoveModelStatePrefix(nameof(CreateRoomInput));
            Tab = "rooms";
            NormalizeTabsAndSearch();

            var ownerUserId = GetCurrentUserId();
            if (ownerUserId == null) return RedirectToPage("/Login");

            if (!ModelState.IsValid)
            {
                OpenEditRoomModal = true;
                await LoadPageDataAsync(ownerUserId.Value, cancellationToken);
                return Page();
            }

            var request = new SaveRoomRequest(
                EditRoomInput.RoomCode,
                EditRoomInput.RoomName,
                EditRoomInput.Capacity,
                EditRoomInput.Location,
                EditRoomInput.Status);

            var result = await _roomManagementService.UpdateAsync(ownerUserId.Value, EditRoomInput.RoomId, request, cancellationToken);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                OpenEditRoomModal = true;
                await LoadPageDataAsync(ownerUserId.Value, cancellationToken);
                return Page();
            }

            SetToast("Thành công", result.Message, "success");
            return RedirectToRoomsTab();
        }

        public async Task<IActionResult> OnPostChangeRoomStatusAsync(int roomId, string status, CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();
            if (ownerUserId == null) return RedirectToPage("/Login");

            var result = await _roomManagementService.SetStatusAsync(ownerUserId.Value, roomId, status, cancellationToken);
            SetToast(result.IsSuccess ? "Thành công" : "Thất bại", result.Message, result.IsSuccess ? "success" : "error");
            return RedirectToRoomsTab();
        }

        public async Task<IActionResult> OnPostDeleteRoomAsync(int roomId, CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();
            if (ownerUserId == null) return RedirectToPage("/Login");

            var result = await _roomManagementService.DeleteRoomAsync(ownerUserId.Value, roomId, cancellationToken);
            SetToast(result.IsSuccess ? "Thành công" : "Thất bại", result.Message, result.IsSuccess ? "success" : "error");
            return RedirectToRoomsTab();
        }

        public async Task<IActionResult> OnPostCreateShiftAsync(CancellationToken cancellationToken)
        {
            RemoveModelStatePrefix("Input");
            RemoveModelStatePrefix(nameof(CreateRoomInput));
            RemoveModelStatePrefix(nameof(EditRoomInput));
            RemoveModelStatePrefix(nameof(EditShiftInput));
            Tab = "shifts";
            NormalizeTabsAndSearch();

            var ownerUserId = GetCurrentUserId();
            if (ownerUserId == null) return RedirectToPage("/Login");

            if (!ModelState.IsValid)
            {
                OpenCreateShiftModal = true;
                await LoadPageDataAsync(ownerUserId.Value, cancellationToken);
                return Page();
            }

            var result = await _shiftManagementService.CreateAsync(ownerUserId.Value, CreateShiftInput, cancellationToken);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                OpenCreateShiftModal = true;
                await LoadPageDataAsync(ownerUserId.Value, cancellationToken);
                return Page();
            }

            SetToast("Thành công", result.Message, "success");
            return RedirectToShiftsTab();
        }

        public async Task<IActionResult> OnPostUpdateShiftAsync(CancellationToken cancellationToken)
        {
            RemoveModelStatePrefix("Input");
            RemoveModelStatePrefix(nameof(CreateRoomInput));
            RemoveModelStatePrefix(nameof(EditRoomInput));
            RemoveModelStatePrefix(nameof(CreateShiftInput));
            Tab = "shifts";
            NormalizeTabsAndSearch();

            var ownerUserId = GetCurrentUserId();
            if (ownerUserId == null) return RedirectToPage("/Login");

            if (!ModelState.IsValid)
            {
                OpenEditShiftModal = true;
                await LoadPageDataAsync(ownerUserId.Value, cancellationToken);
                return Page();
            }

            var request = new SaveShiftRequest(
                EditShiftInput.ShiftCode,
                EditShiftInput.ShiftName,
                EditShiftInput.StartTime,
                EditShiftInput.EndTime,
                EditShiftInput.Status,
                EditShiftInput.Note);

            var result = await _shiftManagementService.UpdateAsync(ownerUserId.Value, EditShiftInput.StudyShiftId, request, cancellationToken);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                OpenEditShiftModal = true;
                await LoadPageDataAsync(ownerUserId.Value, cancellationToken);
                return Page();
            }

            SetToast("Thành công", result.Message, "success");
            return RedirectToShiftsTab();
        }

        public async Task<IActionResult> OnPostChangeShiftStatusAsync(int studyShiftId, string status, CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();
            if (ownerUserId == null) return RedirectToPage("/Login");

            var result = await _shiftManagementService.SetStatusAsync(ownerUserId.Value, studyShiftId, status, cancellationToken);
            SetToast(result.IsSuccess ? "Thành công" : "Thất bại", result.Message, result.IsSuccess ? "success" : "error");
            return RedirectToShiftsTab();
        }

        public async Task<IActionResult> OnPostDeleteShiftAsync(int studyShiftId, CancellationToken cancellationToken)
        {
            var ownerUserId = GetCurrentUserId();
            if (ownerUserId == null) return RedirectToPage("/Login");

            var result = await _shiftManagementService.DeleteShiftAsync(ownerUserId.Value, studyShiftId, cancellationToken);
            SetToast(result.IsSuccess ? "Thành công" : "Thất bại", result.Message, result.IsSuccess ? "success" : "error");
            return RedirectToShiftsTab();
        }

        private async Task LoadPageDataAsync(int ownerUserId, CancellationToken cancellationToken)
        {
            var optionsResult = await _classManagementService.GetClassOptionsAsync(ownerUserId, cancellationToken);
            if (optionsResult.IsSuccess && optionsResult.Value != null)
            {
                CourseOptions = optionsResult.Value.Courses.ToList();
                TeacherOptions = optionsResult.Value.Teachers.ToList();
                RoomOptions = optionsResult.Value.Rooms.ToList();
            }

            var classesResult = await _classManagementService.GetClassesAsync(
                ownerUserId,
                new ClassQuery(Search, ClassStatusFilter, ClassCourseId, ClassTeacherSearch == null ? null : default(int?), ClassPageNumber, ClassPageSize),
                cancellationToken);
            if (classesResult.IsSuccess && classesResult.Value != null)
            {
                Classes = classesResult.Value.Items.ToList();
                TotalClasses = classesResult.Value.TotalItems;
                TotalClassPages = classesResult.Value.TotalPages;
            }

            var roomsResult = await _roomManagementService.GetRoomsAsync(
                ownerUserId,
                new RoomQuery(RoomSearch, RoomStatusFilter, RoomPageNumber, RoomPageSize),
                cancellationToken);
            if (roomsResult.IsSuccess && roomsResult.Value != null)
            {
                Rooms = roomsResult.Value.Items.ToList();
                TotalRooms = roomsResult.Value.TotalItems;
                TotalRoomPages = roomsResult.Value.TotalPages;
            }

            var shiftsResult = await _shiftManagementService.GetShiftsAsync(
                ownerUserId,
                new ShiftQuery(ShiftSearch, ShiftStatusFilter, ShiftPageNumber, ShiftPageSize),
                cancellationToken);
            if (shiftsResult.IsSuccess && shiftsResult.Value != null)
            {
                StudyShifts = shiftsResult.Value.Items.ToList();
                TotalStudyShifts = shiftsResult.Value.TotalItems;
                TotalShiftPages = shiftsResult.Value.TotalPages;
            }
        }

        private int? GetCurrentUserId()
        {
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return userId;
            }
            return null;
        }

        private void SetToast(string title, string message, string type = "success")
        {
            TempData["ToastTitle"] = title;
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = type;
        }

        private IActionResult RedirectToClassesTab()
        {
            return RedirectToPage(new { Tab = "classes" });
        }

        private IActionResult RedirectToRoomsTab()
        {
            return RedirectToPage(new { Tab = "rooms" });
        }

        private IActionResult RedirectToShiftsTab()
        {
            return RedirectToPage(new { Tab = "shifts" });
        }

        private void NormalizeTabsAndSearch()
        {
            Tab = string.IsNullOrWhiteSpace(Tab) ? "classes" : Tab.Trim().ToLowerInvariant();

            if (!ClassPageSizeOptions.Contains(ClassPageSize)) ClassPageSize = 20;
            if (ClassPageNumber < 1) ClassPageNumber = 1;

            if (!RoomPageSizeOptions.Contains(RoomPageSize)) RoomPageSize = 20;
            if (RoomPageNumber < 1) RoomPageNumber = 1;

            if (!ShiftPageSizeOptions.Contains(ShiftPageSize)) ShiftPageSize = 20;
            if (ShiftPageNumber < 1) ShiftPageNumber = 1;

            Search = Search?.Trim();
            ClassTeacherSearch = ClassTeacherSearch?.Trim();
            ClassStatusFilter = string.IsNullOrWhiteSpace(ClassStatusFilter) ? string.Empty : ClassStatusFilter.Trim();

            RoomSearch = RoomSearch?.Trim();
            RoomStatusFilter = string.IsNullOrWhiteSpace(RoomStatusFilter) ? string.Empty : RoomStatusFilter.Trim();

            ShiftSearch = ShiftSearch?.Trim();
            ShiftStatusFilter = string.IsNullOrWhiteSpace(ShiftStatusFilter) ? string.Empty : ShiftStatusFilter.Trim();
        }

        private void RemoveModelStatePrefix(string prefix)
        {
            var keys = ModelState.Keys.Where(k => k.StartsWith(prefix + ".") || k == prefix).ToList();
            foreach (var key in keys)
            {
                ModelState.Remove(key);
            }
        }

        public class RoomEditInput
        {
            public int RoomId { get; set; }
            [Required(ErrorMessage = "Vui lòng nhập mã phòng.")]
            [MaxLength(50)]
            public string RoomCode { get; set; } = string.Empty;
            [Required(ErrorMessage = "Vui lòng nhập tên phòng.")]
            [MaxLength(100)]
            public string RoomName { get; set; } = string.Empty;
            public int? Capacity { get; set; }
            [MaxLength(200)]
            public string? Location { get; set; }
            [Required]
            public string Status { get; set; } = "Active";
        }

        public class ShiftEditInput
        {
            public int StudyShiftId { get; set; }
            [Required(ErrorMessage = "Vui lòng nhập mã ca.")]
            [MaxLength(50)]
            public string ShiftCode { get; set; } = string.Empty;
            [Required(ErrorMessage = "Vui lòng nhập tên ca.")]
            [MaxLength(100)]
            public string ShiftName { get; set; } = string.Empty;
            public TimeOnly StartTime { get; set; }
            public TimeOnly EndTime { get; set; }
            [Required]
            public string Status { get; set; } = "Active";
            [MaxLength(255)]
            public string? Note { get; set; }
        }
    }
}
