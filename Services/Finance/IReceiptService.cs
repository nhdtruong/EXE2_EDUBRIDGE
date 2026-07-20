using System.Threading;
using System.Threading.Tasks;
using EduBridge.Contracts.Finance;

namespace EduBridge.Services.Finance;

public interface IReceiptService
{
    Task<Result<ReceiptResponse>> IssueReceiptAsync(IssueReceiptRequest request, int centerId, int userId, CancellationToken cancellationToken = default);
    Task<Result<ReceiptResponse>> GetByIdAsync(int receiptId, int centerId, CancellationToken cancellationToken = default);
    Task<Result<PagedList<ReceiptResponse>>> GetListAsync(int centerId, ReceiptFilterRequest filter, CancellationToken cancellationToken = default);
    Task<Result> VoidReceiptAsync(int receiptId, int centerId, int userId, string reason, CancellationToken cancellationToken = default);
    Task<Result<ReceiptPrintResponse>> GetForPrintAsync(int receiptId, int centerId, CancellationToken cancellationToken = default);
}
