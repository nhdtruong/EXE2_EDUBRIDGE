using System.ComponentModel.DataAnnotations;

namespace EduBridge.Models.DTOs.TeacherNotification
{
    public class BroadcastNotificationRequest
    {
        [Required(ErrorMessage = "Lớp học là bắt buộc")]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Tiêu đề thông báo là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Nội dung thông báo là bắt buộc")]
        public string Content { get; set; } = null!;
    }

    public class TeacherClassDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = null!;
    }
}
