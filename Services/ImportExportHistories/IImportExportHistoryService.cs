using System.Threading;
using System.Threading.Tasks;
using EduBridge.Contracts.ImportExportHistories;
using EduBridge.Models;

namespace EduBridge.Services.ImportExportHistories;

public interface IImportExportHistoryService
{
    Task<ImportExportHistoryResult<ImportExportHistoryPagedResponse>> GetHistoriesAsync(int ownerUserId, ImportExportHistoryQuery query, CancellationToken cancellationToken = default);
    Task<ImportExportHistory> CreateHistoryAsync(CreateImportExportHistoryRequest request, CancellationToken cancellationToken = default);
    Task UpdateHistoryStatusAsync(int historyId, string status, string? resultFileUrl = null, CancellationToken cancellationToken = default);
}
