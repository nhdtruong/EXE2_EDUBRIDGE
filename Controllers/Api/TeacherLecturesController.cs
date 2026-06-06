using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Models.DTOs.TeacherLectures;

namespace EduBridge.Controllers.Api
{
    [Route("api/teacher/lectures")]
    [ApiController]
    [Authorize(Policy = "TeacherOnly")]
    public class TeacherLecturesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TeacherLecturesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<LecturesResponseDto>> GetLecturesData()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { message = "Không tìm thấy thông tin đăng nhập" });
            }

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
            if (teacher == null)
            {
                return NotFound(new { message = "Không tìm thấy hồ sơ giáo viên" });
            }

            var teacherClasses = await _context.Classes
                .Where(c => c.TeacherId == teacher.TeacherId && c.Status == "Active")
                .ToListAsync();
            
            var classIds = teacherClasses.Select(c => c.ClassId).ToList();

            var classProgresses = new List<ClassProgressDto>();
            foreach (var c in teacherClasses)
            {
                var totalLessons = 20; 
                var completedLessons = await _context.Lessons.CountAsync(l => l.ClassId == c.ClassId);
                
                classProgresses.Add(new ClassProgressDto
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName,
                    CompletedLessons = completedLessons,
                    TotalLessons = totalLessons
                });
            }

            var lessons = await _context.Lessons
                .Include(l => l.Class)
                .Where(l => classIds.Contains(l.ClassId))
                .OrderByDescending(l => l.CreatedAt)
                .Take(20)
                .ToListAsync();

            var lectureHistories = lessons.Select(l => new LectureHistoryDto
            {
                LessonId = l.LessonId,
                DateString = l.CreatedAt.ToString("dd/MM/yyyy"),
                ClassName = l.Class.ClassName,
                Topic = l.LessonTitle,
                Content = l.LessonContent ?? "Không có nội dung",
                Status = "Hoàn thành",
                CreatedAt = l.CreatedAt
            }).ToList();

            var response = new LecturesResponseDto
            {
                ClassProgresses = classProgresses,
                LectureHistories = lectureHistories
            };

            return Ok(response);
        }

        [HttpPost("note")]
        public async Task<IActionResult> AddLectureNote([FromBody] AddLectureNoteRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
            if (teacher == null) return NotFound();

            var classExists = await _context.Classes.AnyAsync(c => c.ClassId == request.ClassId && c.TeacherId == teacher.TeacherId);
            if (!classExists)
            {
                return Forbid();
            }

            var lesson = new Lesson
            {
                ClassId = request.ClassId,
                LessonTitle = request.Topic,
                LessonContent = request.Content,
                LessonDate = DateOnly.FromDateTime(DateTime.Now),
                CreatedAt = DateTime.Now
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Thêm ghi chú thành công", lessonId = lesson.LessonId });
        }
    }
}
