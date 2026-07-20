using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EduBridge.Contracts.ImportExportHistories;
using EduBridge.Data;
using EduBridge.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduBridge.Services.ImportExportHistories;

public sealed class ImportExportHistoryService : IImportExportHistoryService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ImportExportHistoryService> _logger;

    public ImportExportHistoryService(AppDbContext context, ILogger<ImportExportHistoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    private async Task<int?> GetOwnerCenterIdAsync(int ownerUserId, CancellationToken cancellationToken)
    {
        var centerUser = await _context.CenterUsers
            .Where(cu => cu.UserId == ownerUserId && cu.Status == "Active")
            .Select(cu => new { cu.CenterId })
            .FirstOrDefaultAsync(cancellationToken);

        return centerUser?.CenterId;
    }

    public async Task<ImportExportHistoryResult<ImportExportHistoryPagedResponse>> GetHistoriesAsync(int ownerUserId, ImportExportHistoryQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
            if (centerId == null) return ImportExportHistoryResult<ImportExportHistoryPagedResponse>.Failure("Không tìm thấy trung tâm đang hoạt động.");

            var dbQuery = _context.ImportExportHistories
                .Include(h => h.User)
                .Where(h => h.CenterId == centerId.Value);

            if (!string.IsNullOrWhiteSpace(query.EntityName))
            {
                dbQuery = dbQuery.Where(h => h.EntityName == query.EntityName);
            }

            if (!string.IsNullOrWhiteSpace(query.ActionType))
            {
                dbQuery = dbQuery.Where(h => h.ActionType == query.ActionType);
            }

            var totalItems = await dbQuery.CountAsync(cancellationToken);
            var totalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize);
            
            var items = await dbQuery
                .OrderByDescending(h => h.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(h => new ImportExportHistoryListItemResponse(
                    h.HistoryId,
                    h.Title,
                    h.User.FullName,
                    h.ActionType,
                    h.EntityName,
                    h.InputFileUrl,
                    h.ResultFileUrl,
                    h.Status,
                    h.CreatedAt
                )).ToListAsync(cancellationToken);

            var response = new ImportExportHistoryPagedResponse(items, query.Page, query.PageSize, totalItems, totalPages);
            return ImportExportHistoryResult<ImportExportHistoryPagedResponse>.Success(response, "Thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách lịch sử Import/Export");
            return ImportExportHistoryResult<ImportExportHistoryPagedResponse>.Failure($"Lỗi: {ex.Message}");
        }
    }

    public async Task<ImportExportHistory> CreateHistoryAsync(CreateImportExportHistoryRequest request, CancellationToken cancellationToken = default)
    {
        var history = new ImportExportHistory
        {
            CenterId = request.CenterId,
            UserId = request.UserId,
            Title = request.Title,
            ActionType = request.ActionType,
            EntityName = request.EntityName,
            InputFileUrl = request.InputFileUrl,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow
        };

        _context.ImportExportHistories.Add(history);
        await _context.SaveChangesAsync(cancellationToken);
        return history;
    }

    public async Task UpdateHistoryStatusAsync(int historyId, string status, string? resultFileUrl = null, CancellationToken cancellationToken = default)
    {
        var history = await _context.ImportExportHistories.FindAsync(new object[] { historyId }, cancellationToken);
        if (history != null)
        {
            history.Status = status;
            if (!string.IsNullOrWhiteSpace(resultFileUrl))
            {
                history.ResultFileUrl = resultFileUrl;
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
