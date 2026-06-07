using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EduBridge.Models.DTOs.TeacherNotification;

namespace EduBridge.Services.Notifications
{
    public interface INotificationService
    {
        Task<List<TeacherClassDto>> GetTeacherClassesAsync(int teacherUserId, CancellationToken cancellationToken = default);
        Task<bool> BroadcastNotificationAsync(int teacherUserId, BroadcastNotificationRequest request, CancellationToken cancellationToken = default);
    }
}
