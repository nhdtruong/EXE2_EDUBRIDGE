using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using EduBridge.Services.Homeworks;
using EduBridge.Services.Storage;
using EduBridge.Models.DTOs.TeacherHomework;

namespace EduBridge.Pages
{
    public class HomeworkModel : PageModel
    {
        private readonly IHomeworkService _homeworkService;
        private readonly IFileStorageService _storageService;

        public HomeworkModel(IHomeworkService homeworkService, IFileStorageService storageService)
        {
            _homeworkService = homeworkService;
            _storageService = storageService;
        }

        public List<HomeworkListItemDto> HomeworkList { get; set; } = new();
        public List<TeacherClassDto> TeacherClasses { get; set; } = new();
        public List<ParentHomeworkItemDto> ParentHomeworkList { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Login");

            if (User.IsInRole("PARENT"))
            {
                ParentHomeworkList = await _homeworkService.GetParentHomeworksAsync(userId);
            }
            else
            {
                TeacherClasses = await _homeworkService.GetTeacherClassesAsync(userId);
                HomeworkList = await _homeworkService.GetHomeworkListAsync(userId);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync(int lessonId, string title, string? description, DateTime dueDate, IFormFile? pdfFile)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Login");

            if (lessonId <= 0 || string.IsNullOrWhiteSpace(title))
            {
                TempData["ErrorMessage"] = "Thông tin bài tập không hợp lệ.";
                return RedirectToPage();
            }

            string? attachmentUrl = null;
            if (pdfFile != null && pdfFile.Length > 0)
            {
                if (pdfFile.Length > 20 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "File vượt quá kích thước giới hạn (20MB)";
                    return RedirectToPage();
                }

                var extension = System.IO.Path.GetExtension(pdfFile.FileName).ToLower();
                if (extension != ".pdf")
                {
                    TempData["ErrorMessage"] = "Chỉ chấp nhận file định dạng PDF";
                    return RedirectToPage();
                }

                try
                {
                    attachmentUrl = await _storageService.SaveFileAsync(pdfFile, "homeworks");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi khi tải file bài tập lên: " + ex.Message;
                    return RedirectToPage();
                }
            }

            var request = new CreateHomeworkRequest
            {
                LessonId = lessonId,
                Title = title,
                Description = description,
                DueDate = dueDate
            };

            var success = await _homeworkService.CreateHomeworkAsync(userId, request, attachmentUrl);
            if (!success)
            {
                TempData["ErrorMessage"] = "Không thể giao bài tập. Vui lòng kiểm tra lại lớp học/buổi học.";
                return RedirectToPage();
            }

            TempData["ToastMessage"] = "Giao bài tập mới thành công!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostGradeAsync(int homeworkId, int studentId, decimal score, string? feedback)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Login");

            if (homeworkId <= 0 || studentId <= 0)
            {
                TempData["ErrorMessage"] = "Thông tin bài nộp không hợp lệ.";
                return RedirectToPage();
            }

            var request = new GradeSubmissionRequest
            {
                Score = score,
                Feedback = feedback
            };

            var success = await _homeworkService.GradeSubmissionAsync(userId, homeworkId, studentId, request);
            if (!success)
            {
                TempData["ErrorMessage"] = "Không thể chấm điểm bài tập này.";
                return RedirectToPage();
            }

            TempData["ToastMessage"] = "Chấm điểm bài tập thành công!";
            return RedirectToPage();
        }
    }
}
