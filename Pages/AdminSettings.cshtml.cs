using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;
using EduBridge.Models.DTOs;
using EduBridge.Services.Settings;
using System.Linq;

namespace EduBridge.Pages
{
    [Authorize(Roles = "OWNER")]
    public class AdminSettingsModel : PageModel
    {
        private readonly ICenterSettingsService _settingsService;

        public AdminSettingsModel(ICenterSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [BindProperty]
        public CenterSettingsDto SettingsDto { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return RedirectToPage("/Login");
            }

            int ownerId = int.Parse(userIdClaim.Value);

            var settings = await _settingsService.GetSettingsAsync(ownerId);
            if (settings != null)
            {
                SettingsDto = settings;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin nhập.";
                return Page();
            }

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return RedirectToPage("/Login");
            }

            int ownerId = int.Parse(userIdClaim.Value);

            var result = await _settingsService.UpdateSettingsAsync(ownerId, SettingsDto);

            if (result)
            {
                TempData["SuccessMessage"] = "Lưu cấu hình thành công!";
                return RedirectToPage();
            }

            TempData["ErrorMessage"] = "Có lỗi xảy ra khi lưu cấu hình.";
            return Page();
        }
    }
}
