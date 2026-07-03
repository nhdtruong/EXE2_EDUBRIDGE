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
    public class ClassesModel : PageModel
    {
        private readonly AppDbContext _context;

        public ClassesModel(AppDbContext context)
        {
            _context = context;
        }

        public string TeacherName { get; set; } = string.Empty;
        
        // Danh sách các lớp dạy
        public List<ClassItemViewModel> Classes { get; set; } = new();

        // Lớp được chọn chi tiết
        public ClassDetailViewModel? SelectedClass { get; set; }

        public async Task<IActionResult> OnGetAsync(int? classId)
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

            // 1. Lấy danh sách lớp học giáo viên này giảng dạy
            var dbClasses = await _context.Classes
                .Include(c => c.Course)
                .Include(c => c.RoomNavigation)
                .Where(c => c.TeacherId == teacher.TeacherId && !c.IsDeleted)
                .OrderByDescending(c => c.StartDate)
                .ToListAsync();

            // Đếm số lượng học sinh của từng lớp
            var classIds = dbClasses.Select(c => c.ClassId).ToList();
            var studentCounts = await _context.Enrollments
                .Where(e => classIds.Contains(e.ClassId) && e.Status != "Đã nghỉ" && !e.Student.IsDeleted)
                .GroupBy(e => e.ClassId)
                .Select(g => new { ClassId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClassId, x => x.Count);

            Classes = dbClasses.Select(c => new ClassItemViewModel
            {
                ClassId = c.ClassId,
                ClassCode = c.ClassCode,
                ClassName = c.ClassName,
                CourseName = c.Course.CourseName,
                Room = c.RoomNavigation != null ? c.RoomNavigation.RoomName : (c.Room ?? "-"),
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Status = c.Status,
                StudentCount = studentCounts.TryGetValue(c.ClassId, out int count) ? count : 0
            }).ToList();

            // 2. Nếu có classId, lấy thông tin chi tiết và danh sách học sinh
            if (classId.HasValue)
            {
                var selected = dbClasses.FirstOrDefault(c => c.ClassId == classId.Value);
                if (selected != null)
                {
                    // Lấy danh sách học sinh
                    var students = await _context.Enrollments
                        .Include(e => e.Student)
                        .Where(e => e.ClassId == classId.Value && e.Status != "Đã nghỉ" && !e.Student.IsDeleted)
                        .OrderBy(e => e.Student.FullName)
                        .Select(e => new StudentViewModel
                        {
                            StudentId = e.StudentId,
                            StudentCode = e.Student.StudentCode,
                            FullName = e.Student.FullName,
                            DateOfBirth = e.Student.DateOfBirth,
                            Gender = e.Student.Gender ?? "-",
                            PhoneNumber = e.Student.PhoneNumber ?? "-",
                            Email = e.Student.Email ?? "-",
                            EnrollStatus = e.Status
                        })
                        .ToListAsync();

                    SelectedClass = new ClassDetailViewModel
                    {
                        ClassId = selected.ClassId,
                        ClassCode = selected.ClassCode,
                        ClassName = selected.ClassName,
                        CourseName = selected.Course.CourseName,
                        Room = selected.RoomNavigation != null ? selected.RoomNavigation.RoomName : (selected.Room ?? "-"),
                        StartDate = selected.StartDate,
                        EndDate = selected.EndDate,
                        Status = selected.Status,
                        ScheduleText = selected.ScheduleText ?? "-",
                        TotalSessions = selected.TotalSessions,
                        Students = students
                    };
                }
                else
                {
                    // Lớp học không thuộc quyền giảng dạy hoặc đã bị xóa
                    TempData["ErrorMessage"] = "Không tìm thấy lớp học hoặc bạn không có quyền truy cập.";
                    return RedirectToPage();
                }
            }

            ViewData["ActivePage"] = "Classes";
            return Page();
        }
    }

    public class ClassItemViewModel
    {
        public int ClassId { get; set; }
        public string ClassCode { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int StudentCount { get; set; }
    }

    public class ClassDetailViewModel
    {
        public int ClassId { get; set; }
        public string ClassCode { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ScheduleText { get; set; } = string.Empty;
        public int TotalSessions { get; set; }
        public List<StudentViewModel> Students { get; set; } = new();
    }

    public class StudentViewModel
    {
        public int StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateOnly? DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string EnrollStatus { get; set; } = string.Empty;
    }
}
