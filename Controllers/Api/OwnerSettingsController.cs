using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using EduBridge.Models.DTOs;
using EduBridge.Services.Settings;
using System.Linq;

namespace EduBridge.Controllers.Api
{
    [Route("api/v1/owner/settings")]
    [ApiController]
    [Authorize(Roles = "OWNER")]
    public class OwnerSettingsController : ControllerBase
    {
        private readonly ICenterSettingsService _settingsService;

        public OwnerSettingsController(ICenterSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            int ownerId = GetCurrentUserId();

            if (ownerId == 0)
            {
                return Unauthorized(new { success = false, message = "Không xác định được danh tính." });
            }

            var settings = await _settingsService.GetSettingsAsync(ownerId);
            if (settings == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy thông tin trung tâm." });
            }

            return Ok(new { success = true, data = settings });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromBody] CenterSettingsDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ.", errors });
            }

            int ownerId = GetCurrentUserId();

            if (ownerId == 0)
            {
                return Unauthorized(new { success = false, message = "Không xác định được danh tính." });
            }

            var result = await _settingsService.UpdateSettingsAsync(ownerId, dto);

            if (result)
            {
                return Ok(new { success = true, message = "Cập nhật cấu hình thành công." });
            }

            return BadRequest(new { success = false, message = "Lỗi khi cập nhật cấu hình hoặc bạn không có quyền." });
        }
    }
}
