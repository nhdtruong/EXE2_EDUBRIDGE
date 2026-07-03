using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EduBridge.Data;
using EduBridge.Models;

namespace EduBridge.Pages.Teacher
{
    public class ScheduleModel : PageModel
    {
        private readonly AppDbContext _context;

        public ScheduleModel(AppDbContext context)
        {
            _context = context;
        }

        public string TeacherName { get; set; } = string.Empty;
        public int SelectedYear { get; set; }
        public string SelectedWeekRange { get; set; } = string.Empty;

        public List<int> AvailableYears { get; set; } = new();
        public List<WeekItem> Weeks { get; set; } = new();

        public List<DateOnly> DaysOfWeek { get; set; } = new();
        public List<ShiftViewModel> Shifts { get; set; } = new();
        
        // Dictionary lưu trữ Lesson theo DateOnly và (StartTime, EndTime) dưới dạng key: "yyyy-MM-dd_HH:mm-HH:mm"
        public Dictionary<string, List<LessonViewModel>> ScheduleGrid { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? year, string? weekRange)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return RedirectToPage("/Login");

            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.UserId == userId && !t.IsDeleted);

            if (teacher == null)
            {
                return RedirectToPage("/AccessDenied");
            }

            TeacherName = teacher.User.FullName;

            // Thiết lập danh sách năm (năm hiện tại +- 1 năm)
            var currentYear = DateTime.Now.Year;
            AvailableYears = new List<int> { currentYear - 1, currentYear, currentYear + 1 };
            SelectedYear = year ?? currentYear;

            // Lấy danh sách tuần của năm được chọn
            Weeks = GetWeeksOfYear(SelectedYear);

            // Tìm tuần được chọn
            WeekItem selectedWeek;
            if (!string.IsNullOrEmpty(weekRange))
            {
                selectedWeek = Weeks.FirstOrDefault(w => w.RangeValue == weekRange) ?? Weeks.FirstOrDefault(w => w.IsCurrent) ?? Weeks.First();
            }
            else
            {
                selectedWeek = Weeks.FirstOrDefault(w => w.IsCurrent) ?? Weeks.First();
            }

            SelectedWeekRange = selectedWeek.RangeValue;

            // Lấy danh sách 7 ngày trong tuần
            DaysOfWeek = new List<DateOnly>();
            for (int i = 0; i < 7; i++)
            {
                DaysOfWeek.Add(selectedWeek.StartDate.AddDays(i));
            }

            // Lấy các ca học (StudyShifts) của trung tâm
            var dbShifts = await _context.StudyShifts
                .Where(s => s.CenterId == teacher.CenterId && s.Status == "ACTIVE" && !s.IsDeleted)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            Shifts = dbShifts.Select(s => new ShiftViewModel
            {
                StudyShiftId = s.StudyShiftId,
                ShiftCode = s.ShiftCode,
                ShiftName = s.ShiftName,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            }).ToList();

            // Lấy tất cả các buổi dạy (Lessons) của giáo viên trong tuần được chọn
            var start = selectedWeek.StartDate;
            var end = selectedWeek.EndDate;

            var lessons = await _context.Lessons
                .Include(l => l.Class)
                    .ThenInclude(c => c.Course)
                .Include(l => l.Class)
                    .ThenInclude(c => c.RoomNavigation)
                .Include(l => l.ClassSchedule)
                .Where(l => l.Class.TeacherId == teacher.TeacherId
                            && l.LessonDate >= start
                            && l.LessonDate <= end
                            && !l.Class.IsDeleted)
                .ToListAsync();



            // Bổ sung các ca học ảo nếu giáo viên có lịch dạy ngoài ca cố định
            foreach (var lesson in lessons)
            {
                var startTime = lesson.StartTime ?? lesson.ClassSchedule?.StartTime;
                var endTime = lesson.EndTime ?? lesson.ClassSchedule?.EndTime;

                if (startTime.HasValue && endTime.HasValue)
                {
                    var hasShift = Shifts.Any(s => s.StartTime == startTime.Value && s.EndTime == endTime.Value);
                    if (!hasShift)
                    {
                        Shifts.Add(new ShiftViewModel
                        {
                            StudyShiftId = -1,
                            ShiftCode = "CUSTOM",
                            ShiftName = $"Khung giờ khác",
                            StartTime = startTime.Value,
                            EndTime = endTime.Value
                        });
                    }
                }
            }

            // Sắp xếp lại danh sách ca học theo giờ bắt đầu
            Shifts = Shifts.OrderBy(s => s.StartTime).ToList();

            // Điền dữ liệu vào Grid: Key dạng "yyyy-MM-dd_HH:mm-HH:mm"
            foreach (var lesson in lessons)
            {
                var startTime = lesson.StartTime ?? lesson.ClassSchedule?.StartTime;
                var endTime = lesson.EndTime ?? lesson.ClassSchedule?.EndTime;

                if (startTime.HasValue && endTime.HasValue)
                {
                    var dateStr = lesson.LessonDate.ToString("yyyy-MM-dd");
                    var timeStr = $"{startTime.Value:hh\\:mm}-{endTime.Value:hh\\:mm}";
                    var gridKey = $"{dateStr}_{timeStr}";

                    // Kiểm tra xem đã có bản ghi điểm danh nào cho buổi này chưa
                    var isAttended = await _context.Attendances.AnyAsync(a => a.LessonId == lesson.LessonId);

                    var viewModel = new LessonViewModel
                    {
                        LessonId = lesson.LessonId,
                        ClassId = lesson.ClassId,
                        ClassCode = lesson.Class.ClassCode,
                        ClassName = lesson.Class.ClassName,
                        CourseName = lesson.Class.Course.CourseName,
                        Room = lesson.Class.RoomNavigation != null ? lesson.Class.RoomNavigation.RoomName : (lesson.Class.Room ?? "-"),
                        LessonTitle = lesson.LessonTitle,
                        StartTime = startTime.Value,
                        EndTime = endTime.Value,
                        IsAttended = isAttended
                    };

                    if (!ScheduleGrid.ContainsKey(gridKey))
                    {
                        ScheduleGrid[gridKey] = new List<LessonViewModel>();
                    }
                    ScheduleGrid[gridKey].Add(viewModel);
                }
            }

            ViewData["ActivePage"] = "Schedule";
            return Page();
        }



        private List<WeekItem> GetWeeksOfYear(int year)
        {
            var weeks = new List<WeekItem>();
            var firstDayOfYear = new DateTime(year, 1, 1);
            
            // Tìm ngày Thứ Hai đầu tiên của năm (hoặc trước đó)
            int diff = (7 + (firstDayOfYear.DayOfWeek - DayOfWeek.Monday)) % 7;
            var currentMonday = firstDayOfYear.AddDays(-1 * diff);
            
            var today = DateTime.Today;

            while (currentMonday.Year == year || currentMonday.AddDays(6).Year == year)
            {
                var currentSunday = currentMonday.AddDays(6);
                var start = DateOnly.FromDateTime(currentMonday);
                var end = DateOnly.FromDateTime(currentSunday);
                
                var isCurrent = today >= currentMonday && today <= currentSunday;

                weeks.Add(new WeekItem
                {
                    StartDate = start,
                    EndDate = end,
                    RangeValue = $"{start:yyyy-MM-dd} To {end:yyyy-MM-dd}",
                    Label = $"{start:dd/MM} To {end:dd/MM}",
                    IsCurrent = isCurrent
                });
                currentMonday = currentMonday.AddDays(7);
            }

            return weeks;
        }
    }

    public class WeekItem
    {
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string RangeValue { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }
    }

    public class ShiftViewModel
    {
        public int StudyShiftId { get; set; }
        public string ShiftCode { get; set; } = string.Empty;
        public string ShiftName { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }

    public class LessonViewModel
    {
        public int LessonId { get; set; }
        public int ClassId { get; set; }
        public string ClassCode { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public string LessonTitle { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool IsAttended { get; set; }
    }
}
