using EduBridge.Contracts.Dashboard;
using EduBridge.Services.Classes;
using EduBridge.Models.DTOs.TeacherDashboard;

namespace EduBridge.Services.Dashboard;

public interface IDashboardService
{
    Task<ClassOperationResult<DashboardSummaryResponse>> GetDashboardSummaryAsync(
        int ownerUserId,
        CancellationToken cancellationToken = default);

    Task<ClassOperationResult<DashboardResponseDto>> GetTeacherDashboardDataAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
