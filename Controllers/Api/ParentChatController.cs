using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using EduBridge.Contracts.Classes;
using EduBridge.Models.DTOs.ParentApp;
using EduBridge.Services.ParentApp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EduBridge.Services.Storage;
using System.IO;

namespace EduBridge.Controllers.Api;

[ApiController]
[Route("api/v1/parent/chat")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ParentOnly")]
public class ParentChatController : ControllerBase
{
    private readonly IParentAppService _parentAppService;
    private readonly IFileStorageService _storageService;

    public ParentChatController(IParentAppService parentAppService, IFileStorageService storageService)
    {
        _parentAppService = parentAppService;
        _storageService = storageService;
    }

    [HttpPost("read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead([FromQuery] int contactUserId, CancellationToken cancellationToken)
    {
        var parentId = GetUserId();
        if (parentId == 0) return Unauthorized();
        var result = await _parentAppService.MarkChatAsReadAsync(parentId, contactUserId, cancellationToken);
        return Ok(new ApiResponse<bool>(true, result.Message, result.Value));
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<object>>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0) return BadRequest(new ApiResponse<object>(false, "Không có ảnh được chọn.", null));
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".gif"))
            return BadRequest(new ApiResponse<object>(false, "Chỉ hỗ trợ ảnh JPG, PNG hoặc GIF.", null));
        var fileUrl = await _storageService.SaveFileAsync(file, "chat", cancellationToken);
        return Ok(new ApiResponse<object>(true, "Tải ảnh thành công.", new { fileUrl, fileName = file.FileName }));
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
}
