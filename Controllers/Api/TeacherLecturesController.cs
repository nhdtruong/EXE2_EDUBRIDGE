using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using EduBridge.Models.DTOs.TeacherLectures;
using EduBridge.Services.Lectures;
using EduBridge.Contracts.Classes;

namespace EduBridge.Controllers.Api
{
    [Route("api/v1/teacher/lectures")]
    [ApiController]
    [Authorize(Policy = "TeacherOnly")]
    public class TeacherLecturesController : ControllerBase
    {
        private readonly ILectureService _lectureService;

        public TeacherLecturesController(ILectureService lectureService)
        {
            _lectureService = lectureService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<LecturesResponseDto>>> GetLecturesData()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new ApiResponse<LecturesResponseDto>(false, "Không tìm thấy thông tin đăng nhập", null));
            }

            var response = await _lectureService.GetLecturesDataAsync(userId);
            return Ok(new ApiResponse<LecturesResponseDto>(true, "Success", response));
        }

        [HttpPost("note")]
        public async Task<ActionResult<ApiResponse<bool>>> AddLectureNote([FromBody] AddLectureNoteRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized(new ApiResponse<bool>(false, "Chưa đăng nhập", false));

            var result = await _lectureService.AddLectureNoteAsync(userId, request);
            if (!result)
            {
                return BadRequest(new ApiResponse<bool>(false, "Không thể thêm ghi chú bài giảng", false));
            }

            return Ok(new ApiResponse<bool>(true, "Thêm ghi chú thành công", true));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> EditLectureNote(int id, [FromBody] EditLectureNoteRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized(new ApiResponse<bool>(false, "Chưa đăng nhập", false));

            var result = await _lectureService.EditLectureNoteAsync(userId, id, request);
            if (!result)
            {
                return BadRequest(new ApiResponse<bool>(false, "Không thể cập nhật ghi chú bài giảng", false));
            }

            return Ok(new ApiResponse<bool>(true, "Cập nhật ghi chú thành công", true));
        }
    }
}
