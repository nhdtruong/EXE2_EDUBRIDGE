using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EduBridge.Contracts.Classes;
using EduBridge.Models.DTOs.TeacherGrades;
using EduBridge.Services.Grades;

namespace EduBridge.Controllers.Api
{
    [Route("api/v1/teacher/grades")]
    [ApiController]
    [Authorize(Policy = "TeacherOnly")]
    public class TeacherGradesController : ControllerBase
    {
        private readonly IGradeService _gradeService;

        public TeacherGradesController(IGradeService gradeService)
        {
            _gradeService = gradeService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<StudentGradesDto>>>> GetGrades([FromQuery] int classId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new ApiResponse<List<StudentGradesDto>>(false, "Không tìm thấy thông tin đăng nhập", null));
            }

            var response = await _gradeService.GetGradesByClassAsync(userId, classId);
            return Ok(new ApiResponse<List<StudentGradesDto>>(true, "Success", response));
        }

        [HttpPost("save")]
        public async Task<ActionResult<ApiResponse<bool>>> SaveGrades([FromBody] SaveStudentGradesRequest request)
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

            var result = await _gradeService.SaveStudentGradesAsync(userId, request);
            if (!result)
            {
                return BadRequest(new ApiResponse<bool>(false, "Không thể lưu điểm số học sinh này", false));
            }

            return Ok(new ApiResponse<bool>(true, "Cập nhật điểm số thành công", true));
        }
    }
}
