using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EduBridge.Models.DTOs.TeacherGrades;

namespace EduBridge.Services.Grades
{
    public interface IGradeService
    {
        Task<List<TeacherClassDto>> GetTeacherClassesAsync(int teacherUserId, CancellationToken cancellationToken = default);
        Task<List<StudentGradesDto>> GetGradesByClassAsync(int teacherUserId, int classId, CancellationToken cancellationToken = default);
        
        Task<bool> SaveStudentGradesAsync(int teacherUserId, SaveStudentGradesRequest request, CancellationToken cancellationToken = default);
    }
}
