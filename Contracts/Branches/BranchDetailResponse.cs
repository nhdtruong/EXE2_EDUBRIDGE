namespace EduBridge.Contracts.Branches;

public class BranchDetailResponse
{
    public int BranchId { get; set; }
    public int CenterId { get; set; }
    public string BranchName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string BranchCode { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string? LogoUrl { get; set; }
    public string? ImageUrl { get; set; }
}
