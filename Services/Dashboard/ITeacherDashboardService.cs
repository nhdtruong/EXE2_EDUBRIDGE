using EduBridge.Contracts.Dashboard;
using EduBridge.Services.Classes;

namespace EduBridge.Services.Dashboard;

public interface ITeacherDashboardService
{
    Task<ClassOperationResult<TeacherDashboardSummaryResponse>> GetDashboardSummaryAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
