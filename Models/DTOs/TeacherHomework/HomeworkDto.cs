using System;
using System.ComponentModel.DataAnnotations;

namespace EduBridge.Models.DTOs.TeacherHomework
{
    public class TeacherClassDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
    }

    public class HomeworkListItemDto
    {
        public int HomeworkId { get; set; }
        public int LessonId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CreatedAtString { get; set; } = string.Empty;
        public string DueDateString { get; set; } = string.Empty;
        public string? AttachmentUrl { get; set; }
        public int SubmittedCount { get; set; }
        public int TotalStudents { get; set; }
        public int GradedCount { get; set; }
        public int PendingCount { get; set; }
    }

    public class CreateHomeworkRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn bài học.")]
        public int LessonId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề bài tập.")]
        [StringLength(200, ErrorMessage = "Tiêu đề không quá 200 ký tự.")]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hạn nộp.")]
        public DateTime DueDate { get; set; }

        public string? AttachmentUrl { get; set; }
    }

    public class HomeworkSubmissionListItemDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public int? SubmissionId { get; set; }
        public string? SubmissionContent { get; set; }
        public string? SubmissionFileUrl { get; set; }
        public string Status { get; set; } = "NotSubmitted"; // NotSubmitted, Submitted, Graded
        public decimal? Score { get; set; }
        public string? Feedback { get; set; }
        public string? SubmittedAtString { get; set; }
    }

    public class GradeSubmissionRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập điểm số.")]
        [Range(0, 10, ErrorMessage = "Điểm số phải từ 0 đến 10.")]
        public decimal Score { get; set; }

        public string? Feedback { get; set; }
    }

    public class LessonDropdownOptionDto
    {
        public int LessonId { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
        public string DateString { get; set; } = string.Empty;
    }

    public class ParentHomeworkItemDto
    {
        public int HomeworkId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string LessonTitle { get; set; } = string.Empty;
        public string? AttachmentUrl { get; set; }
        public DateTime? DueDate { get; set; }
        public string DueDateString { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int? SubmissionId { get; set; }
        public string? SubmissionContent { get; set; }
        public string? SubmissionFileUrl { get; set; }
        public string? SubmittedAtString { get; set; }
        public string Status { get; set; } = "NotSubmitted"; // NotSubmitted, Submitted, Graded, Overdue
        public decimal? Score { get; set; }
        public string? Feedback { get; set; }
    }

    public class SubmitHomeworkRequestDto
    {
        [Required(ErrorMessage = "Vui lòng chọn bài tập.")]
        public int HomeworkId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn học sinh.")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Vui lòng đính kèm file bài làm.")]
        public string SubmissionFileUrl { get; set; } = string.Empty;

        public string? SubmissionContent { get; set; }
    }
}
