using System.Threading;
using System.Threading.Tasks;
using EduBridge.Models.DTOs.TeacherLectures;

namespace EduBridge.Services.Lectures
{
    public interface ILectureService
    {
        Task<LecturesResponseDto> GetLecturesDataAsync(int teacherUserId, CancellationToken cancellationToken = default);
        Task<bool> AddLectureNoteAsync(int teacherUserId, AddLectureNoteRequest request, CancellationToken cancellationToken = default);
        Task<bool> EditLectureNoteAsync(int teacherUserId, int lessonId, EditLectureNoteRequest request, CancellationToken cancellationToken = default);
    }
}
