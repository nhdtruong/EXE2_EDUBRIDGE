using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EduBridge.Contracts.Classes;
using EduBridge.Models.DTOs.TeacherHomework;
using EduBridge.Services.Homeworks;

namespace EduBridge.Controllers.Api
{
    [Route("api/v1/teacher/homework")]
    [ApiController]
    [Authorize(Policy = "TeacherOnly")]
    public class TeacherHomeworkController : ControllerBase
    {
        private readonly IHomeworkService _homeworkService;

        public TeacherHomeworkController(IHomeworkService homeworkService)
        {
            _homeworkService = homeworkService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<HomeworkListItemDto>>>> GetHomeworkList()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new ApiResponse<List<HomeworkListItemDto>>(false, "Không tìm thấy thông tin đăng nhập", null));
            }

            var response = await _homeworkService.GetHomeworkListAsync(userId);
            return Ok(new ApiResponse<List<HomeworkListItemDto>>(true, "Success", response));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<bool>>> CreateHomework([FromBody] CreateHomeworkRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized(new ApiResponse<bool>(false, "Chưa đăng nhập", false));

            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<bool>(false, "Dữ liệu không hợp lệ", false));
            }

            var result = await _homeworkService.CreateHomeworkAsync(userId, request);
            if (!result)
            {
                return BadRequest(new ApiResponse<bool>(false, "Không thể tạo bài tập mới", false));
            }

            return Ok(new ApiResponse<bool>(true, "Giao bài tập thành công", true));
        }

        [HttpGet("{id}/submissions")]
        public async Task<ActionResult<ApiResponse<List<HomeworkSubmissionListItemDto>>>> GetSubmissions(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new ApiResponse<List<HomeworkSubmissionListItemDto>>(false, "Không tìm thấy thông tin đăng nhập", null));
            }

            var response = await _homeworkService.GetSubmissionsAsync(userId, id);
            return Ok(new ApiResponse<List<HomeworkSubmissionListItemDto>>(true, "Success", response));
        }

        [HttpPut("{id}/submissions/{studentId}/grade")]
        public async Task<ActionResult<ApiResponse<bool>>> GradeSubmission(int id, int studentId, [FromBody] GradeSubmissionRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized(new ApiResponse<bool>(false, "Chưa đăng nhập", false));

            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<bool>(false, "Dữ liệu không hợp lệ", false));
            }

            var result = await _homeworkService.GradeSubmissionAsync(userId, id, studentId, request);
            if (!result)
            {
                return BadRequest(new ApiResponse<bool>(false, "Không thể chấm điểm bài tập này", false));
            }

            return Ok(new ApiResponse<bool>(true, "Chấm điểm thành công", true));
        }

        [HttpGet("lessons")]
        public async Task<ActionResult<ApiResponse<List<LessonDropdownOptionDto>>>> GetLessons([FromQuery] int classId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new ApiResponse<List<LessonDropdownOptionDto>>(false, "Không tìm thấy thông tin đăng nhập", null));
            }

            var response = await _homeworkService.GetLessonsByClassAsync(userId, classId);
            return Ok(new ApiResponse<List<LessonDropdownOptionDto>>(true, "Success", response));
        }
    }
}
