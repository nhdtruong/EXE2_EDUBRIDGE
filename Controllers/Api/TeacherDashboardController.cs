using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using EduBridge.Models.DTOs.TeacherDashboard;
using EduBridge.Services.Dashboard;

namespace EduBridge.Controllers.Api
{
    [Route("api/teacher/dashboard")]
    [ApiController]
    [Authorize(Policy = "TeacherOnly")]
    public class TeacherDashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public TeacherDashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<ActionResult<DashboardResponseDto>> GetDashboardData()
        {
            // 1. Get logged-in user ID
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                // Return unauthorized if user id is missing
                return Unauthorized(new { message = "Không tìm thấy thông tin đăng nhập" });
            }

            var result = await _dashboardService.GetTeacherDashboardDataAsync(userId);
            if (!result.IsSuccess)
            {
                return NotFound(new { message = result.Message });
            }

            return Ok(result.Value);
        }
    }
}
