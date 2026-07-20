using System.ComponentModel.DataAnnotations;

namespace EduBridge.Models.DTOs
{
    public class CenterSettingsDto
    {
        public GeneralSettingsDto General { get; set; } = new();
        public FinanceSettingsDto Finance { get; set; } = new();
    }

    public class GeneralSettingsDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tên trung tâm")]
        public string CenterName { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        // Múi giờ, ví dụ "Asia/Ho_Chi_Minh"
        public string TimeZone { get; set; } = "Asia/Ho_Chi_Minh";

        // Ngày làm việc trong tuần (VD: "2,3,4,5,6,7" -> Thứ 2 đến thứ 7)
        public string WorkingDays { get; set; } = "2,3,4,5,6,7";

        public string? LogoUrl { get; set; }
    }

    public class FinanceSettingsDto
    {
        // Số ngày đến hạn thanh toán hóa đơn
        [Range(0, 365, ErrorMessage = "Số ngày đến hạn phải từ 0 đến 365 ngày")]
        public int DefaultInvoiceDueDays { get; set; } = 7;

        // Các phương thức thanh toán được hỗ trợ (CASH, TRANSFER, ...)
        public string PaymentMethods { get; set; } = "CASH,TRANSFER";

        // Định dạng mã biên lai. Ví dụ: {PREFIX}-{YYYYMMDD}-{ID}
        public string ReceiptNumberingFormat { get; set; } = "BL-{YYYYMMDD}-{ID}";

        // Tiền tệ
        public string Currency { get; set; } = "VND";

        // Có hiển thị thuế/VAT trên hóa đơn không
        public bool ShowTaxOnInvoice { get; set; } = false;
    }
}
