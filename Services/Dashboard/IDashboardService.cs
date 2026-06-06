using EduBridge.Contracts.Dashboard;
using EduBridge.Services.Classes;

namespace EduBridge.Services.Dashboard;

public interface IDashboardService
{
    Task<ClassOperationResult<DashboardSummaryResponse>> GetDashboardSummaryAsync(
        int ownerUserId,
        CancellationToken cancellationToken = default);
}
