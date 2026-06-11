using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EduBridge.Models.DTOs.TeacherAttendance;

namespace EduBridge.Services.Attendance
{
    public interface IAttendanceService
    {
        Task<List<TeacherClassDto>> GetTeacherClassesAsync(int teacherUserId, CancellationToken cancellationToken = default);
        
        Task<List<LessonDropdownDto>> GetLessonsByClassAsync(int teacherUserId, int classId, CancellationToken cancellationToken = default);
        
        Task<List<StudentAttendanceDto>> GetAttendanceByLessonAsync(int teacherUserId, int lessonId, CancellationToken cancellationToken = default);
        
        Task<bool> SaveAttendanceAsync(int teacherUserId, SaveAttendanceRequest request, CancellationToken cancellationToken = default);
        
        Task<List<AttendanceHistoryDto>> GetAttendanceHistoryAsync(int teacherUserId, int classId, CancellationToken cancellationToken = default);
    }
}
