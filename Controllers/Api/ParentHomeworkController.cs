using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EduBridge.Contracts.Classes;
using EduBridge.Models.DTOs.TeacherHomework;
using EduBridge.Services.Homeworks;
using EduBridge.Services.Storage;

namespace EduBridge.Controllers.Api
{
    [Route("api/v1/parent/homework")]
    [ApiController]
    [Authorize(Policy = "ParentOnly")]
    public class ParentHomeworkController : ControllerBase
    {
        private readonly IHomeworkService _homeworkService;
        private readonly IFileStorageService _storageService;

        public ParentHomeworkController(IHomeworkService homeworkService, IFileStorageService storageService)
        {
            _homeworkService = homeworkService;
            _storageService = storageService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ParentHomeworkItemDto>>>> GetHomeworkList()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new ApiResponse<List<ParentHomeworkItemDto>>(false, "Không tìm thấy thông tin đăng nhập", null));
            }

            var response = await _homeworkService.GetParentHomeworksAsync(userId);
            return Ok(new ApiResponse<List<ParentHomeworkItemDto>>(true, "Success", response));
        }

        [HttpPost("upload")]
        public async Task<ActionResult<ApiResponse<object>>> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiResponse<object>(false, "Không có file nào được chọn", null));
            }

            // Giới hạn 20MB
            if (file.Length > 20 * 1024 * 1024)
            {
                return BadRequest(new ApiResponse<object>(false, "File vượt quá kích thước giới hạn (20MB)", null));
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            var blockedExtensions = new List<string> { ".exe", ".bat", ".cmd", ".sh", ".asp", ".aspx", ".php", ".js", ".vbs" };
            if (blockedExtensions.Contains(extension))
            {
                return BadRequest(new ApiResponse<object>(false, "Định dạng file không được phép", null));
            }

            try
            {
                var fileUrl = await _storageService.SaveFileAsync(file, "homework_submissions");

                return Ok(new ApiResponse<object>(true, "Tải lên file thành công", new
                {
                    fileUrl,
                    fileName = file.FileName
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>(false, ex.Message ?? "Lỗi tải lên file.", null));
            }
        }

        [HttpPost("submit")]
        public async Task<ActionResult<ApiResponse<bool>>> SubmitHomework([FromBody] SubmitHomeworkRequestDto request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new ApiResponse<bool>(false, "Chưa đăng nhập", false));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<bool>(false, "Dữ liệu không hợp lệ", false));
            }

            var result = await _homeworkService.SubmitHomeworkAsync(userId, request);
            if (!result)
            {
                return BadRequest(new ApiResponse<bool>(false, "Không thể nộp bài tập. Vui lòng kiểm tra lại hạn nộp hoặc thông tin học sinh.", false));
            }

            return Ok(new ApiResponse<bool>(true, "Nộp bài tập thành công", true));
        }
    }
}
