using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using EduBridge.Services.Grades;
using EduBridge.Models.DTOs.TeacherGrades;

namespace EduBridge.Pages
{
    public class GradesModel : PageModel
    {
        private readonly IGradeService _gradeService;

        public GradesModel(IGradeService gradeService)
        {
            _gradeService = gradeService;
        }

        public List<TeacherClassDto> TeacherClasses { get; set; } = new();
        public List<StudentGradesDto> StudentsGrades { get; set; } = new();
        public int SelectedClassId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? classId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Login");

            TeacherClasses = await _gradeService.GetTeacherClassesAsync(userId);

            if (!TeacherClasses.Any())
            {
                return Page();
            }

            SelectedClassId = classId ?? TeacherClasses.First().ClassId;

            // Xác thực lớp học này thuộc về giáo viên
            if (TeacherClasses.All(c => c.ClassId != SelectedClassId))
            {
                SelectedClassId = TeacherClasses.First().ClassId;
            }

            StudentsGrades = await _gradeService.GetGradesByClassAsync(userId, SelectedClassId);

            return Page();
        }
    }
}
