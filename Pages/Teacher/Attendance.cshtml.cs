using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using EduBridge.Services.Attendance;
using EduBridge.Models.DTOs.TeacherAttendance;

namespace EduBridge.Pages.Teacher
{
    public class AttendanceModel : PageModel
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceModel(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        public List<TeacherClassDto> TeacherClasses { get; set; } = new();
        public List<LessonDropdownDto> ClassLessons { get; set; } = new();
        public List<StudentAttendanceDto> StudentAttendances { get; set; } = new();
        public List<AttendanceHistoryDto> AttendanceHistory { get; set; } = new();

        public int SelectedClassId { get; set; }
        public int SelectedLessonId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? classId, int? lessonId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Login");

            TeacherClasses = await _attendanceService.GetTeacherClassesAsync(userId);

            if (!TeacherClasses.Any())
            {
                return Page();
            }

            SelectedClassId = classId ?? TeacherClasses.First().ClassId;

            // Xác thực lớp học thuộc về giáo viên
            if (TeacherClasses.All(c => c.ClassId != SelectedClassId))
            {
                SelectedClassId = TeacherClasses.First().ClassId;
            }

            // Lấy danh sách buổi học của lớp
            ClassLessons = await _attendanceService.GetLessonsByClassAsync(userId, SelectedClassId);

            if (ClassLessons.Any())
            {
                SelectedLessonId = lessonId ?? ClassLessons.First().LessonId;

                // Xác thực buổi học thuộc lớp học này
                if (ClassLessons.All(l => l.LessonId != SelectedLessonId))
                {
                    SelectedLessonId = ClassLessons.First().LessonId;
                }

                // Lấy bảng điểm danh của buổi học
                StudentAttendances = await _attendanceService.GetAttendanceByLessonAsync(userId, SelectedLessonId);
            }

            // Lấy lịch sử điểm danh của lớp
            AttendanceHistory = await _attendanceService.GetAttendanceHistoryAsync(userId, SelectedClassId);

            return Page();
        }
    }
}
