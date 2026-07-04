using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EduBridge.Models;
using EduBridge.Services.Lectures;
using EduBridge.Models.DTOs.TeacherLectures;

namespace EduBridge.Pages.Teacher
{
    public class LecturesModel : PageModel
    {
        private readonly ILectureService _lectureService;

        public LecturesModel(ILectureService lectureService)
        {
            _lectureService = lectureService;
        }

        public List<ClassProgressViewModel> ClassesProgress { get; set; } = new();
        public List<LectureHistoryViewModel> LectureHistories { get; set; } = new();

        [BindProperty]
        public AddNoteInputModel Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Login");

            var serviceData = await _lectureService.GetLecturesDataAsync(userId);

            ClassesProgress = serviceData.ClassProgresses.Select(cp => new ClassProgressViewModel
            {
                ClassId = cp.ClassId,
                ClassName = cp.ClassName,
                CompletedLessons = cp.CompletedLessons,
                TotalLessons = cp.TotalLessons
            }).ToList();

            LectureHistories = serviceData.LectureHistories.Select(lh => new LectureHistoryViewModel
            {
                LessonId = lh.LessonId,
                ClassId = lh.ClassId,
                DateString = lh.DateString,
                ClassName = lh.ClassName,
                Topic = lh.Topic,
                Content = lh.Content,
                Status = lh.Status,
                SessionNumber = lh.SessionNumber,
                StartTime = lh.StartTime,
                EndTime = lh.EndTime,
                Homeworks = lh.Homeworks.Select(h => new LessonHomeworkViewModel
                {
                    HomeworkId = h.HomeworkId,
                    Title = h.Title,
                    Description = h.Description,
                    DueDateString = h.DueDateString,
                    AttachmentUrl = h.AttachmentUrl
                }).ToList()
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAddNoteAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Login");

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

            var request = new AddLectureNoteRequest
            {
                ClassId = Input.ClassId,
                Topic = Input.Topic,
                Content = Input.Content,
                Status = string.IsNullOrWhiteSpace(Input.Status) ? "Scheduled" : Input.Status
            };

            var success = await _lectureService.AddLectureNoteAsync(userId, request);
            if (!success)
            {
                TempData["ErrorMessage"] = "Lớp học không hợp lệ hoặc bạn không có quyền ghi chú lớp này.";
                return RedirectToPage();
            }

            TempData["ToastMessage"] = "Thêm ghi chú bài giảng thành công!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditNoteAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToPage("/Login");

            if (Input.LessonId <= 0)
            {
                TempData["ErrorMessage"] = "Không xác định được bài giảng cần sửa.";
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

            var request = new EditLectureNoteRequest
            {
                Topic = Input.Topic,
                Content = Input.Content,
                Status = string.IsNullOrWhiteSpace(Input.Status) ? "Scheduled" : Input.Status
            };

            var success = await _lectureService.EditLectureNoteAsync(userId, Input.LessonId, request);
            if (!success)
            {
                TempData["ErrorMessage"] = "Không thể cập nhật bài giảng. Bài giảng không tồn tại hoặc bạn không có quyền sửa.";
                return RedirectToPage();
            }

            TempData["ToastMessage"] = "Cập nhật bài giảng thành công!";
            return RedirectToPage();
        }
    }

    public class ClassProgressViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int CompletedLessons { get; set; }
        public int TotalLessons { get; set; }
        public int PercentComplete => TotalLessons == 0 ? 0 : (int)((double)CompletedLessons / TotalLessons * 100);
    }

    public class LessonHomeworkViewModel
    {
        public int HomeworkId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DueDateString { get; set; } = string.Empty;
        public string AttachmentUrl { get; set; } = string.Empty;
    }

    public class LectureHistoryViewModel
    {
        public int LessonId { get; set; }
        public int ClassId { get; set; }
        public string DateString { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? SessionNumber { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public List<LessonHomeworkViewModel> Homeworks { get; set; } = new();
    }

    public class AddNoteInputModel
    {
        public int LessonId { get; set; }
        public int ClassId { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = "Scheduled";
    }
}
