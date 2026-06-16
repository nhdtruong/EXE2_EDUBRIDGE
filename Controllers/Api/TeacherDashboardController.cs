using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using EduBridge.Models.DTOs.TeacherDashboard;
using EduBridge.Services.Dashboard;
using EduBridge.Contracts.Classes;

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
        public async Task<ActionResult<ApiResponse<DashboardResponseDto>>> GetDashboardData()
        {
            // 1. Get logged-in user ID
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                // Return unauthorized if user id is missing
                return Unauthorized(new ApiResponse<DashboardResponseDto>(false, "Không tìm thấy thông tin đăng nhập", null));
            }

            var result = await _dashboardService.GetTeacherDashboardDataAsync(userId);
            if (!result.IsSuccess)
            {
                return NotFound(new ApiResponse<DashboardResponseDto>(false, result.Message, null));
            }

<<<<<<< HEAD
            return Ok(new ApiResponse<DashboardResponseDto>(true, result.Message, result.Value));
=======
            return Ok(result.Value);
>>>>>>> e5417bb24ce6b520875746ee3d72982295df8d14
        }
    }
}
