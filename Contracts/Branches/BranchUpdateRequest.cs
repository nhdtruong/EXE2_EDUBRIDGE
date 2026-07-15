using System.ComponentModel.DataAnnotations;

namespace EduBridge.Contracts.Branches;

public class BranchUpdateRequest
{
    [Required]
    public int BranchId { get; set; }

    public int CenterId { get; set; }

    [Required(ErrorMessage = "Tên cơ sở là bắt buộc")]
    [MaxLength(150, ErrorMessage = "Tên cơ sở tối đa 150 ký tự")]
    public string BranchName { get; set; } = null!;

    [Required(ErrorMessage = "Email cơ sở là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    [MaxLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
    public string PhoneNumber { get; set; } = null!;

    [Required(ErrorMessage = "Mã cơ sở là bắt buộc")]
    [MaxLength(50, ErrorMessage = "Mã cơ sở tối đa 50 ký tự")]
    public string BranchCode { get; set; } = null!;

    [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
    [MaxLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự")]
    public string Address { get; set; } = null!;

    public Microsoft.AspNetCore.Http.IFormFile? Logo { get; set; }

    public Microsoft.AspNetCore.Http.IFormFile? CenterImage { get; set; }
}
