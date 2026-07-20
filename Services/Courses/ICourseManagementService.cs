using EduBridge.Services.Classes;
using EduBridge.Contracts.Courses;

namespace EduBridge.Services.Courses;

public interface ICourseManagementService
{
    Task<ClassOperationResult<CoursePagedResponse>> GetCoursesAsync(int ownerUserId, CourseQuery query, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<CourseResponse>> GetCourseAsync(int ownerUserId, int courseId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<CourseMutationResponse>> CreateCourseAsync(int ownerUserId, SaveCourseRequest request, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<CourseMutationResponse>> UpdateCourseAsync(int ownerUserId, int courseId, SaveCourseRequest request, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<CourseMutationResponse>> SetStatusAsync(int ownerUserId, int courseId, string status, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<bool>> DeleteCourseAsync(int ownerUserId, int courseId, CancellationToken cancellationToken = default);
}
