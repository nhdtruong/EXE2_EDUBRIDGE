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
        
        Task<List<EduBridge.Models.DTOs.Shared.NotificationDto>> GetMyNotificationsAsync(int userId, int limit = 20, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
        Task<bool> MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default);
        Task<bool> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
    }
}
