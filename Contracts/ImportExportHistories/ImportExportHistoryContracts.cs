using System;
using System.Collections.Generic;

namespace EduBridge.Contracts.ImportExportHistories;

public sealed class ImportExportHistoryQuery
{
    public string? EntityName { get; set; }
    public string? ActionType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed record ImportExportHistoryListItemResponse(
    int HistoryId,
    string Title,
    string ActorName,
    string ActionType,
    string EntityName,
    string? InputFileUrl,
    string? ResultFileUrl,
    string Status,
    DateTime CreatedAt);

public sealed record ImportExportHistoryPagedResponse(
    IReadOnlyList<ImportExportHistoryListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed class CreateImportExportHistoryRequest
{
    public int CenterId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? InputFileUrl { get; set; }
    public string Status { get; set; } = "Pending";
}
