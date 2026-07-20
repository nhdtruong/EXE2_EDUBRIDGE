using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EduBridge.Contracts.Finance;

namespace EduBridge.Services.Finance;

public interface IFinanceSummaryService
{
    Task<Result<DashboardFinanceSummaryResponse>> GetDashboardSummaryAsync(int centerId, int month, int year, CancellationToken cancellationToken = default);
    Task<Result<List<ClassDebtSummaryResponse>>> GetClassDebtSummariesAsync(int centerId, CancellationToken cancellationToken = default);
}
