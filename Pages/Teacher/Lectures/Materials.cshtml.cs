using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EduBridge.Data;
using EduBridge.Models;

namespace EduBridge.Pages.Teacher.Lectures
{
    public class MaterialsModel : PageModel
    {
        private readonly AppDbContext _context;

        public MaterialsModel(AppDbContext context)
        {
            _context = context;
        }

        public class MaterialDto
        {
            public int HomeworkId { get; set; }
            public string Title { get; set; } = null!;
            public string? Description { get; set; }
            public DateTime? DueDate { get; set; }
            public string? AttachmentUrl { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public Lesson? DisplayLesson { get; set; }
        public List<MaterialDto> Materials { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int lessonId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Login");

            DisplayLesson = await _context.Lessons
                .Include(l => l.Class)
                    .ThenInclude(c => c.Course)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId);

            if (DisplayLesson == null)
            {
                return RedirectToPage("/NotFound");
            }

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
            if (teacher == null || DisplayLesson.Class.TeacherId != teacher.TeacherId || DisplayLesson.Class.IsDeleted)
            {
                return RedirectToPage("/AccessDenied");
            }

            Materials = await _context.Homeworks
                .Where(h => h.LessonId == lessonId)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new MaterialDto
                {
                    HomeworkId = h.HomeworkId,
                    Title = h.Title,
                    Description = h.Description,
                    DueDate = h.DueDate,
                    AttachmentUrl = h.AttachmentUrl,
                    CreatedAt = h.CreatedAt
                })
                .ToListAsync();

            return Page();
        }
    }
}
