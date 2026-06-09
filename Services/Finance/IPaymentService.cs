using System.Threading;
using System.Threading.Tasks;
using EduBridge.Contracts.Finance;

namespace EduBridge.Services.Finance;

public interface IPaymentService
{
    Task<Result<PaymentResponse>> CreatePaymentAsync(CreatePaymentRequest request, int centerId, int userId, CancellationToken cancellationToken = default);
    Task<Result<PagedList<PaymentResponse>>> GetListAsync(int centerId, PaymentFilterRequest filter, CancellationToken cancellationToken = default);
    Task<Result> CancelPaymentAsync(int paymentId, int centerId, int userId, string reason, CancellationToken cancellationToken = default);
}
