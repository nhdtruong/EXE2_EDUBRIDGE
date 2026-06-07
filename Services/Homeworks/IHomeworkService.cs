using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EduBridge.Models.DTOs.TeacherHomework;

namespace EduBridge.Services.Homeworks
{
    public interface IHomeworkService
    {
        Task<List<HomeworkListItemDto>> GetHomeworkListAsync(int teacherUserId, CancellationToken cancellationToken = default);
        
        Task<bool> CreateHomeworkAsync(int teacherUserId, CreateHomeworkRequest request, CancellationToken cancellationToken = default);
        
        Task<List<HomeworkSubmissionListItemDto>> GetSubmissionsAsync(int teacherUserId, int homeworkId, CancellationToken cancellationToken = default);
        
        Task<bool> GradeSubmissionAsync(int teacherUserId, int homeworkId, int studentId, GradeSubmissionRequest request, CancellationToken cancellationToken = default);
        
        Task<List<LessonDropdownOptionDto>> GetLessonsByClassAsync(int teacherUserId, int classId, CancellationToken cancellationToken = default);
    }
}
