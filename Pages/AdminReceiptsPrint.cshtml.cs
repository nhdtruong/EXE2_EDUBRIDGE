using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EduBridge.Contracts.Finance;
using EduBridge.Services.Finance;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using EduBridge.Data;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Pages
{
    [Authorize(Roles = "OWNER")]
    public class AdminReceiptsPrintModel : PageModel
    {
        private readonly IReceiptService _receiptService;
        private readonly AppDbContext _context;

        public AdminReceiptsPrintModel(IReceiptService receiptService, AppDbContext context)
        {
            _receiptService = receiptService;
            _context = context;
        }

        public ReceiptPrintResponse ReceiptData { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var ownerUserId))
                return RedirectToPage("/Login");

            var center = await _context.Centers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.OwnerUserId == ownerUserId && c.Status == "Active");

            if (center == null) return RedirectToPage("/Login");
            
            var centerId = center.CenterId;

            var result = await _receiptService.GetForPrintAsync(id, centerId);
            
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
