using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EduBridge.Contracts.Finance;
using EduBridge.Services.Finance;
using EduBridge.Services.Settings;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Threading;

namespace EduBridge.Pages
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminReceiptsPrintModel : PageModel
    {
        private readonly IReceiptService _receiptService;
        private readonly ICenterSettingsService _centerSettingsService;

        public AdminReceiptsPrintModel(IReceiptService receiptService, ICenterSettingsService centerSettingsService)
        {
            _receiptService = receiptService;
            _centerSettingsService = centerSettingsService;
        }

        public ReceiptPrintResponse ReceiptData { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var ownerUserId))
                return RedirectToPage("/Login");

            var centerId = await _centerSettingsService.GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
            if (centerId == null) return RedirectToPage("/Login");

            var result = await _receiptService.GetForPrintAsync(id, centerId.Value);
            
            if (!result.IsSuccess || result.Value == null)
            {
                TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy dữ liệu phiếu thu.";
                return RedirectToPage("/AdminFinance");
            }

            ReceiptData = result.Value;
            return Page();
        }
    }
}
