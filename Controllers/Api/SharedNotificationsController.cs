using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using EduBridge.Models.DTOs.Shared;
using EduBridge.Services.Notifications;
using EduBridge.Contracts.Classes;
using System.Threading;

namespace EduBridge.Controllers.Api
{
    [Route("api/v1/shared/notifications")]
    [ApiController]
    [Authorize]
    public class SharedNotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public SharedNotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private int GetUserId()
        {
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return userId;
            }
            return 0;
        }

        [HttpGet("my")]
        public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetMyNotifications([FromQuery] int limit = 20, CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized(new ApiResponse<List<NotificationDto>>(false, "Chưa đăng nhập", null));

            var notifications = await _notificationService.GetMyNotificationsAsync(userId, limit, cancellationToken);
            return Ok(new ApiResponse<List<NotificationDto>>(true, "Thành công", notifications));
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount(CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized(new ApiResponse<int>(false, "Chưa đăng nhập", 0));

            var count = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);
            return Ok(new ApiResponse<int>(true, "Thành công", count));
        }

        [HttpPut("{id:int}/read")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(int id, CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized(new ApiResponse<bool>(false, "Chưa đăng nhập", false));

            var success = await _notificationService.MarkAsReadAsync(id, userId, cancellationToken);
            return Ok(new ApiResponse<bool>(success, success ? "Đã đánh dấu đã đọc" : "Không tìm thấy thông báo", success));
        }

        [HttpPut("read-all")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAllAsRead(CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized(new ApiResponse<bool>(false, "Chưa đăng nhập", false));

            var success = await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);
            return Ok(new ApiResponse<bool>(success, "Đã đánh dấu tất cả đã đọc", success));
        }
    }
}
