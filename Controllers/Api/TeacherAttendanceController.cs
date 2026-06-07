using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EduBridge.Contracts.Classes;
using EduBridge.Models.DTOs.TeacherAttendance;
using EduBridge.Services.Attendance;

namespace EduBridge.Controllers.Api
{
    [Route("api/v1/teacher/attendance")]
    [ApiController]
    [Authorize(Policy = "TeacherOnly")]
    public class TeacherAttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;

        public TeacherAttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        [HttpGet("lessons")]
        public async Task<ActionResult<ApiResponse<List<LessonDropdownDto>>>> GetLessons([FromQuery] int classId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new ApiResponse<List<LessonDropdownDto>>(false, "Không tìm thấy thông tin đăng nhập", null));
            }

            var response = await _attendanceService.GetLessonsByClassAsync(userId, classId);
            return Ok(new ApiResponse<List<LessonDropdownDto>>(true, "Success", response));
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<StudentAttendanceDto>>>> GetAttendance([FromQuery] int lessonId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new ApiResponse<List<StudentAttendanceDto>>(false, "Không tìm thấy thông tin đăng nhập", null));
            }

            var response = await _attendanceService.GetAttendanceByLessonAsync(userId, lessonId);
            return Ok(new ApiResponse<List<StudentAttendanceDto>>(true, "Success", response));
        }

        [HttpPost("save")]
        public async Task<ActionResult<ApiResponse<bool>>> SaveAttendance([FromBody] SaveAttendanceRequest request)
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

            var result = await _attendanceService.SaveAttendanceAsync(userId, request);
            if (!result)
            {
                return BadRequest(new ApiResponse<bool>(false, "Không thể lưu điểm danh cho buổi học này", false));
            }

            return Ok(new ApiResponse<bool>(true, "Lưu điểm danh thành công", true));
        }

        [HttpGet("history")]
        public async Task<ActionResult<ApiResponse<List<AttendanceHistoryDto>>>> GetHistory([FromQuery] int classId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new ApiResponse<List<AttendanceHistoryDto>>(false, "Không tìm thấy thông tin đăng nhập", null));
            }

            var response = await _attendanceService.GetAttendanceHistoryAsync(userId, classId);
            return Ok(new ApiResponse<List<AttendanceHistoryDto>>(true, "Success", response));
        }
    }
}
