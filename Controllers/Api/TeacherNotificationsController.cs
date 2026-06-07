using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using EduBridge.Models.DTOs.TeacherNotification;
using EduBridge.Services.Notifications;
using EduBridge.Contracts.Classes;

namespace EduBridge.Controllers.Api
{
    [Route("api/v1/teacher/notifications")]
    [ApiController]
    [Authorize(Policy = "TeacherOnly")]
    public class TeacherNotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public TeacherNotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("classes")]
        public async Task<ActionResult<ApiResponse<List<TeacherClassDto>>>> GetClasses()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new ApiResponse<List<TeacherClassDto>>(false, "Chưa đăng nhập", null));
            }

            try
            {
                var classes = await _notificationService.GetTeacherClassesAsync(userId);
                return Ok(new ApiResponse<List<TeacherClassDto>>(true, "Success", classes));
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<TeacherClassDto>>(false, $"Lỗi hệ thống: {ex.Message}", null));
            }
        }

        [HttpPost("broadcast")]
        public async Task<ActionResult<ApiResponse<bool>>> BroadcastNotification([FromBody] BroadcastNotificationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<bool>(false, "Dữ liệu không hợp lệ", false));
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new ApiResponse<bool>(false, "Chưa đăng nhập", false));
            }

            try
            {
                var result = await _notificationService.BroadcastNotificationAsync(userId, request);
                if (!result)
                {
                    return BadRequest(new ApiResponse<bool>(false, "Không thể gửi thông báo chung (Lớp học không hợp lệ hoặc không có quyền)", false));
                }

                return Ok(new ApiResponse<bool>(true, "Gửi thông báo chung thành công", true));
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new ApiResponse<bool>(false, $"Lỗi hệ thống: {ex.Message}", false));
            }
        }
    }
}
