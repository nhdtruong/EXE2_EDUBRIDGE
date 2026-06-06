using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EduBridge.Data;
using EduBridge.Models;
using System.ComponentModel.DataAnnotations;

namespace EduBridge.Pages.Teacher
{
    public class LecturesModel : PageModel
    {
        private readonly AppDbContext _context;

        public LecturesModel(AppDbContext context)
        {
            _context = context;
        }

        public List<ClassProgressViewModel> ClassesProgress { get; set; } = new();
        public List<LectureHistoryViewModel> LectureHistories { get; set; } = new();
        public List<Class> TeacherClasses { get; set; } = new();

        [BindProperty]
        public AddNoteInputModel Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Login");

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
            if (teacher == null) return RedirectToPage("/Login");

            TeacherClasses = await _context.Classes
                .Where(c => c.TeacherId == teacher.TeacherId && c.Status == "Active")
                .ToListAsync();

            var classIds = TeacherClasses.Select(c => c.ClassId).ToList();

            // Tính tiến độ: DurationWeeks từ Course là tổng số tuần → ước tính tổng bài
            var classesWithCourse = await _context.Classes
                .Include(c => c.Course)
                .Where(c => classIds.Contains(c.ClassId))
                .ToListAsync();

            foreach (var c in classesWithCourse)
            {
                var completedLessons = await _context.Lessons.CountAsync(l => l.ClassId == c.ClassId);
                // Dùng DurationWeeks của Course nếu có, mặc định 20 nếu không
                var totalLessons = c.Course?.DurationWeeks ?? 20;

                ClassesProgress.Add(new ClassProgressViewModel
                {
                    ClassName = c.ClassName,
                    CompletedLessons = completedLessons,
                    TotalLessons = totalLessons
                });
            }

            // Lịch sử bài giảng - lấy về in-memory để format
            var rawLessons = await _context.Lessons
                .Include(l => l.Class)
                .Where(l => classIds.Contains(l.ClassId))
                .OrderByDescending(l => l.CreatedAt)
                .Take(20)
                .ToListAsync();

            LectureHistories = rawLessons.Select(l => new LectureHistoryViewModel
            {
                LessonId = l.LessonId,
                DateString = l.LessonDate.ToString("dd/MM/yyyy"),
                ClassName = l.Class.ClassName,
                Topic = l.LessonTitle,
                Content = l.LessonContent ?? "Không có nội dung",
                Status = "Hoàn thành"
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAddNoteAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Login");

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
            if (teacher == null) return RedirectToPage("/Login");

            // Validate thủ công: ClassId phải > 0
            if (Input.ClassId <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn lớp học.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(Input.Topic))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập chủ đề bài giảng.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(Input.Content))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập nội dung ghi chú.";
                return RedirectToPage();
            }

            // Kiểm tra lớp học có thuộc giáo viên này không
            var classExists = await _context.Classes
                .AnyAsync(c => c.ClassId == Input.ClassId && c.TeacherId == teacher.TeacherId);

            if (!classExists)
            {
                TempData["ErrorMessage"] = "Lớp học không hợp lệ hoặc bạn không có quyền ghi chú lớp này.";
                return RedirectToPage();
            }

            var lesson = new Lesson
            {
                ClassId = Input.ClassId,
                LessonTitle = Input.Topic.Trim(),
                LessonContent = Input.Content.Trim(),
                LessonDate = DateOnly.FromDateTime(DateTime.Now),
                CreatedAt = DateTime.Now
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = "Thêm ghi chú bài giảng thành công!";
            return RedirectToPage();
        }
    }

    public class ClassProgressViewModel
    {
        public string ClassName { get; set; } = string.Empty;
        public int CompletedLessons { get; set; }
        public int TotalLessons { get; set; }
        public int PercentComplete => TotalLessons == 0 ? 0 : (int)((double)CompletedLessons / TotalLessons * 100);
    }

    public class LectureHistoryViewModel
    {
        public int LessonId { get; set; }
        public string DateString { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class AddNoteInputModel
    {
        public int ClassId { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
