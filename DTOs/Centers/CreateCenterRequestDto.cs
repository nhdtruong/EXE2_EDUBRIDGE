using System.ComponentModel.DataAnnotations;

namespace EduBridge.DTOs.Centers;

public class CreateCenterRequestDto
{
    [Required(ErrorMessage = "Mã trung tâm không được để trống")]
    [StringLength(50, ErrorMessage = "Mã trung tâm không được vượt quá 50 ký tự")]
    [RegularExpression(@"^[a-zA-Z0-9\-]+$", ErrorMessage = "Mã trung tâm chỉ được chứa chữ, số và dấu gạch ngang (-)")]
    public string CenterCode { get; set; } = null!;

    [Required(ErrorMessage = "Tên trung tâm không được để trống")]
    [StringLength(150, ErrorMessage = "Tên trung tâm không được vượt quá 150 ký tự")]
    public string CenterName { get; set; } = null!;

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(150, ErrorMessage = "Email không được vượt quá 150 ký tự")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
    public string? PhoneNumber { get; set; }

    [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự")]
    public string? Address { get; set; }

    public int? ProjectId { get; set; }

    public Microsoft.AspNetCore.Http.IFormFile? Logo { get; set; }

    public string Status { get; set; } = "Active";
}
