using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Services.Lectures;
using EduBridge.Models.DTOs.TeacherLectures;

namespace EduBridge.Pages.Teacher.Lectures
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILectureService _lectureService;

        public EditModel(AppDbContext context, ILectureService lectureService)
        {
            _context = context;
            _lectureService = lectureService;
        }

        [BindProperty]
        public EditLectureInputModel Input { get; set; } = new();

        public Lesson DisplayLesson { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int lessonId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Login");

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId && !t.IsDeleted);
            if (teacher == null) return RedirectToPage("/AccessDenied");

            var lesson = await _context.Lessons
                .Include(l => l.Class)
                .ThenInclude(c => c.Course)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId && !l.Class.IsDeleted);

            if (lesson == null || lesson.Class.TeacherId != teacher.TeacherId)
            {
                TempData["ErrorMessage"] = "Bài giảng không tồn tại hoặc bạn không có quyền truy cập.";
                return RedirectToPage("/Teacher/Lectures");
            }

            DisplayLesson = lesson;

            Input.LessonId = lesson.LessonId;
            Input.Topic = lesson.LessonTitle ?? string.Empty;
            Input.Content = lesson.LessonContent ?? string.Empty;
            Input.Status = lesson.Status;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Login");

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId && !t.IsDeleted);
            if (teacher == null) return RedirectToPage("/AccessDenied");

            var lesson = await _context.Lessons
                .Include(l => l.Class)
                .FirstOrDefaultAsync(l => l.LessonId == Input.LessonId && !l.Class.IsDeleted);

            if (lesson == null || lesson.Class.TeacherId != teacher.TeacherId)
            {
                TempData["ErrorMessage"] = "Không thể cập nhật bài giảng. Bài giảng không tồn tại hoặc bạn không có quyền sửa.";
                return RedirectToPage("/Teacher/Lectures");
            }

            DisplayLesson = lesson;

            if (string.IsNullOrWhiteSpace(Input.Topic))
            {
                ModelState.AddModelError("Input.Topic", "Vui lòng nhập chủ đề bài giảng.");
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Input.Content))
            {
                ModelState.AddModelError("Input.Content", "Vui lòng nhập nội dung ghi chú.");
                return Page();
            }

            var request = new EditLectureNoteRequest
            {
                Topic = Input.Topic,
                Content = Input.Content,
                Status = string.IsNullOrWhiteSpace(Input.Status) ? "Scheduled" : Input.Status
            };

            var success = await _lectureService.EditLectureNoteAsync(userId, Input.LessonId, request);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Không thể cập nhật bài giảng. Vui lòng thử lại.");
                return Page();
            }

            TempData["ToastMessage"] = "Cập nhật bài giảng thành công!";
            return RedirectToPage("/Teacher/Lectures");
        }
    }

    public class EditLectureInputModel
    {
        public int LessonId { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = "Scheduled";
    }
}
