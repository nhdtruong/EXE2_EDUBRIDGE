using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using EduBridge.Contracts.Finance;

namespace EduBridge.Services.Finance;

public interface IInvoiceService
{
    Task<Result<InvoiceResponse>> CreateInvoiceAsync(CreateInvoiceRequest request, int centerId, int userId, CancellationToken cancellationToken = default);
    Task<Result<InvoiceDetailResponse>> GetByIdAsync(int invoiceId, int centerId, CancellationToken cancellationToken = default);
    Task<Result<PagedList<InvoiceResponse>>> GetListAsync(int centerId, InvoiceFilterRequest filter, CancellationToken cancellationToken = default);
    Task<Result> CancelAsync(int invoiceId, int centerId, int userId, string reason, CancellationToken cancellationToken = default);
    Task<Result> UpdateStatusFromPaymentsAsync(int invoiceId, CancellationToken cancellationToken = default);
    Task<Result<PagedList<StudentDebtResponse>>> GetDebtListAsync(int centerId, DebtFilterRequest filter, CancellationToken cancellationToken = default);
    Task<string> GenerateInvoiceCodeAsync(int centerId, CancellationToken cancellationToken = default);
}
