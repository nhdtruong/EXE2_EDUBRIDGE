using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EduBridge.Contracts.Classes; // Contains ApiResponse wrapper
using EduBridge.Models.DTOs.TeacherChat;
using EduBridge.Services.Chat;

namespace EduBridge.Controllers.Api
{
    [Route("api/v1/teacher/chat")]
    [ApiController]
    [Authorize(Policy = "TeacherOnly")]
    public class TeacherChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IWebHostEnvironment _env;

        public TeacherChatController(IChatService chatService, IWebHostEnvironment env)
        {
            _chatService = chatService;
            _env = env;
        }

        [HttpGet("conversations")]
        public async Task<ActionResult<ApiResponse<List<ConversationDto>>>> GetConversations()
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out int userId))
                {
                    return Unauthorized(new ApiResponse<List<ConversationDto>>(false, "Chưa đăng nhập", null));
                }

                var conversations = await _chatService.GetTeacherConversationsAsync(userId);
                return Ok(new ApiResponse<List<ConversationDto>>(true, "Success", conversations));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<ConversationDto>>(false, $"Lỗi máy chủ: {ex.Message}", null));
            }
        }

        [HttpGet("history")]
        public async Task<ActionResult<ApiResponse<List<ChatMessageDto>>>> GetChatHistory([FromQuery] int contactUserId)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out int userId))
                {
                    return Unauthorized(new ApiResponse<List<ChatMessageDto>>(false, "Chưa đăng nhập", null));
                }

                var history = await _chatService.GetChatHistoryAsync(userId, contactUserId);
                return Ok(new ApiResponse<List<ChatMessageDto>>(true, "Success", history));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<ChatMessageDto>>(false, $"Lỗi máy chủ: {ex.Message}", null));
            }
        }

        [HttpPost("read")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead([FromQuery] int contactUserId)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out int userId))
                {
                    return Unauthorized(new ApiResponse<bool>(false, "Chưa đăng nhập", false));
                }

                var result = await _chatService.MarkAsReadAsync(userId, contactUserId);
                return Ok(new ApiResponse<bool>(true, "Đã đánh dấu đã đọc", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<bool>(false, $"Lỗi máy chủ: {ex.Message}", false));
            }
        }

        [HttpPost("upload")]
        public async Task<ActionResult<ApiResponse<object>>> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiResponse<object>(false, "Không có file nào được chọn", null));
            }

            // Giới hạn 10MB
            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new ApiResponse<object>(false, "File vượt quá kích thước giới hạn (10MB)", null));
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            var blockedExtensions = new List<string> { ".exe", ".bat", ".cmd", ".sh", ".asp", ".aspx", ".php", ".js", ".vbs" };
            if (blockedExtensions.Contains(extension))
            {
                return BadRequest(new ApiResponse<object>(false, "Định dạng file không được phép", null));
            }

            try
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "chat");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsDir, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var fileUrl = $"/uploads/chat/{uniqueFileName}";
                return Ok(new ApiResponse<object>(true, "Tải lên file thành công", new
                {
                    fileUrl,
                    fileName = file.FileName
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>(false, $"Lỗi tải lên file: {ex.Message}", null));
            }
        }
    }
}
