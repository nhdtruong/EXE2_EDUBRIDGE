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
}
