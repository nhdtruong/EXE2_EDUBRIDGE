using System.ComponentModel.DataAnnotations;

namespace EduBridge.Contracts.Courses;

public class SaveCourseRequest
{
    [Required(ErrorMessage = "Mã môn là bắt buộc.")]
    [MaxLength(30, ErrorMessage = "Mã môn tối đa 30 ký tự.")]
    public string CourseCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên môn là bắt buộc.")]
    [MaxLength(150, ErrorMessage = "Tên môn tối đa 150 ký tự.")]
    public string CourseName { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
    public string? Description { get; set; }

    [Range(1, 1000, ErrorMessage = "Tổng số buổi phải từ 1 đến 1000.")]
    public int? TotalSessions { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Học phí không được âm.")]
    public decimal? TuitionFee { get; set; }

    public string Status { get; set; } = "Active";
}

public class UpdateCourseStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}
