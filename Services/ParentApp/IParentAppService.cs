using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EduBridge.Contracts.Classes;
using EduBridge.Models.DTOs.ParentApp;

namespace EduBridge.Services.ParentApp;

public interface IParentAppService
{
    Task<ClassOperationResult<ParentDashboardDto>> GetDashboardAsync(int parentUserId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<List<ParentChildOverviewDto>>> GetChildrenAsync(int parentUserId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<ParentChildDetailDto>> GetChildDetailAsync(int parentUserId, int studentId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<List<ParentScheduleDto>>> GetScheduleAsync(int parentUserId, int? studentId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<List<ParentAttendanceDto>>> GetAttendanceAsync(int parentUserId, int studentId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<List<ParentGradeDto>>> GetGradesAsync(int parentUserId, int studentId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<List<ParentHomeworkDto>>> GetHomeworksAsync(int parentUserId, int studentId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<List<ParentInvoiceDto>>> GetInvoicesAsync(int parentUserId, int? studentId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<List<ParentNotificationDto>>> GetNotificationsAsync(int parentUserId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<bool>> MarkNotificationAsReadAsync(int parentUserId, int notificationId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<List<ParentChatConversationDto>>> GetChatConversationsAsync(int parentUserId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<List<ParentChatMessageDto>>> GetChatMessagesAsync(int parentUserId, int teacherUserId, CancellationToken cancellationToken = default);
}
