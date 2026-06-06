using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Models.DTOs.TeacherDashboard;

namespace EduBridge.Controllers.Api
{
    [Route("api/teacher/dashboard")]
    [ApiController]
    [Authorize(Policy = "TeacherOnly")]
    public class TeacherDashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TeacherDashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<DashboardResponseDto>> GetDashboardData()
        {
            // 1. Get logged-in user ID
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                // Return unauthorized if user id is missing
                return Unauthorized(new { message = "Không tìm thấy thông tin đăng nhập" });
            }

            // 2. Find the Teacher record
            var teacher = await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null)
            {
                return NotFound(new { message = "Không tìm thấy hồ sơ giáo viên tương ứng với tài khoản" });
            }

            // 3. Gather Dashboard stats
            // Lấy danh sách lớp do giáo viên này phụ trách
            var teacherClasses = await _context.Classes
                .Where(c => c.TeacherId == teacher.TeacherId && c.Status == "Active")
                .ToListAsync();
            
            var classIds = teacherClasses.Select(c => c.ClassId).ToList();

            // Tổng số học sinh duy nhất trong các lớp của giáo viên
            var totalStudents = await _context.Enrollments
                .Where(e => classIds.Contains(e.ClassId) && e.Status == "Đang học")
                .Select(e => e.StudentId)
                .Distinct()
                .CountAsync();

            // Lịch dạy hôm nay (Simplified: Lấy vài lịch dạy của các lớp này)
            // Ghi chú: Thực tế cần filter theo ClassSchedule.DayOfWeek
            var schedules = await _context.ClassSchedules
                .Include(cs => cs.Class)
                .Where(cs => classIds.Contains(cs.ClassId))
                .ToListAsync();

            var todaySchedulesDto = schedules.Take(5).Select(s => new ScheduleDto
            {
                ClassId = s.ClassId,
                ClassName = s.Class.ClassName,
                Topic = "Bài giảng", // Có thể join với Course để lấy tên bài
                TimeRange = $"{s.StartTime} - {s.EndTime}",
                Room = s.Class.Room ?? string.Empty
            }).ToList();

            // Bài tập gần đây (Homeworks thuộc các Lesson của các Lớp này)
            var lessons = await _context.Lessons
                .Where(l => classIds.Contains(l.ClassId))
                .Select(l => l.LessonId)
                .ToListAsync();

            var recentHomeworks = await _context.Homeworks
                .Include(h => h.Lesson)
                .ThenInclude(l => l.Class)
                .Where(h => lessons.Contains(h.LessonId))
                .OrderByDescending(h => h.CreatedAt)
                .Take(5)
                .ToListAsync();

            var assignmentsDto = recentHomeworks.Select(h => new AssignmentDto
            {
                HomeworkId = h.HomeworkId,
                Title = h.Title,
                ClassName = h.Lesson.Class.ClassName,
                CreatedAt = h.CreatedAt,
                SubmittedCount = 0, // Tính số bài nộp (nếu có table Submissions)
                TotalStudents = _context.Enrollments.Count(e => e.ClassId == h.Lesson.ClassId && e.Status == "Đang học")
            }).ToList();

            // Đếm số tin nhắn chưa đọc
            var unreadMessagesCount = await _context.Messages
                .CountAsync(m => m.ReceiverUserId == userId && m.IsRead == false);

            // 5 tin nhắn gần nhất
            var recentMessages = await _context.Messages
                .Include(m => m.SenderUser)
                .ThenInclude(u => u.Role)
                .Where(m => m.ReceiverUserId == userId)
                .OrderByDescending(m => m.SentAt)
                .Take(5)
                .Select(m => new MessageDto
                {
                    MessageId = m.MessageId,
                    SenderName = m.SenderUser.FullName,
                    SenderRole = m.SenderUser.Role.RoleName,
                    ShortContent = m.Content.Length > 50 ? m.Content.Substring(0, 50) + "..." : m.Content,
                    SentAt = m.SentAt
                })
                .ToListAsync();

            // 4. Map vào Response DTO
            var response = new DashboardResponseDto
            {
                TeacherName = teacher.User.FullName,
                TotalClasses = teacherClasses.Count,
                TotalStudents = totalStudents,
                UngradedAssignmentsCount = 0, // Placeholder
                UnreadMessagesCount = unreadMessagesCount,
                TodaySchedules = todaySchedulesDto,
                RecentAssignments = assignmentsDto,
                RecentMessages = recentMessages
            };

            return Ok(response);
        }
    }
}
