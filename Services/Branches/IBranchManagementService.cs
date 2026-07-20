using System.Threading;
using System.Threading.Tasks;
using EduBridge.Contracts.Branches;

namespace EduBridge.Services.Branches;

public interface IBranchManagementService
{
    Task<BranchOperationResult<int>> CreateBranchAsync(BranchCreateRequest request, CancellationToken cancellationToken = default);
    Task<BranchDetailResponse?> GetBranchDetailAsync(int branchId, int centerId, CancellationToken cancellationToken = default);
    Task<BranchOperationResult<bool>> UpdateBranchAsync(int branchId, BranchUpdateRequest request, CancellationToken cancellationToken = default);
}
