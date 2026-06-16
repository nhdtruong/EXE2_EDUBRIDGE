<<<<<<< HEAD
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EduBridge.Contracts.Classes; // Contains ApiResponse wrapper
using EduBridge.Models.DTOs.TeacherChat;
using EduBridge.Services.Chat;
using EduBridge.Services.Storage;

namespace EduBridge.Controllers.Api
{
    [Route("api/v1/parent/chat")]
    [ApiController]
    [Authorize(Policy = "ParentOnly")]
    public class ParentChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IFileStorageService _storageService;

        public ParentChatController(IChatService chatService, IFileStorageService storageService)
        {
            _chatService = chatService;
            _storageService = storageService;
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

                var conversations = await _chatService.GetParentConversationsAsync(userId);
                return Ok(new ApiResponse<List<ConversationDto>>(true, "Success", conversations));
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<List<ConversationDto>>(false, "Đã xảy ra lỗi hệ thống trong quá trình xử lý.", null));
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
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<List<ChatMessageDto>>(false, "Đã xảy ra lỗi hệ thống trong quá trình xử lý.", null));
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
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<bool>(false, "Đã xảy ra lỗi hệ thống trong quá trình xử lý.", false));
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
                var fileUrl = await _storageService.SaveFileAsync(file, "chat");

                return Ok(new ApiResponse<object>(true, "Tải lên file thành công", new
                {
                    fileUrl,
                    fileName = file.FileName
                }));
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<object>(false, "Lỗi tải lên file.", null));
            }
        }
    }
=======
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using EduBridge.Services.Classes;
using EduBridge.Contracts.Classes;
using EduBridge.Models.DTOs.ParentApp;
using EduBridge.Services.ParentApp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/v1/parent/chat")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ParentOnly")]
public class ParentChatController : ControllerBase
{
    private readonly IParentAppService _parentAppService;

    public ParentChatController(IParentAppService parentAppService)
    {
        _parentAppService = parentAppService;
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<ApiResponse<List<ParentChatConversationDto>>>> GetConversations(CancellationToken cancellationToken)
    {
        var parentId = GetUserId();
        if (parentId == 0) return Unauthorized();

        var result = await _parentAppService.GetChatConversationsAsync(parentId, cancellationToken);
        
        return result.IsSuccess
            ? Ok(new ApiResponse<List<ParentChatConversationDto>>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<List<ParentChatConversationDto>>(false, result.Message, null, result.Errors));
    }

    [HttpGet("messages/{receiverId:int}")]
    public async Task<ActionResult<ApiResponse<List<ParentChatMessageDto>>>> GetMessages(int receiverId, CancellationToken cancellationToken)
    {
        var parentId = GetUserId();
        if (parentId == 0) return Unauthorized();

        var result = await _parentAppService.GetChatMessagesAsync(parentId, receiverId, cancellationToken);
        
        return result.IsSuccess
            ? Ok(new ApiResponse<List<ParentChatMessageDto>>(true, result.Message, result.Value))
            : BadRequest(new ApiResponse<List<ParentChatMessageDto>>(false, result.Message, null, result.Errors));
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
>>>>>>> e5417bb24ce6b520875746ee3d72982295df8d14
}
