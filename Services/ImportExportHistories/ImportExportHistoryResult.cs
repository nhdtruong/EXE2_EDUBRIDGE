using System.Collections.Generic;

namespace EduBridge.Services.ImportExportHistories;

public sealed class ImportExportHistoryResult<T>
{
    private ImportExportHistoryResult(
        bool isSuccess,
        string message,
        T? value,
        IReadOnlyDictionary<string, string[]> errors)
    {
        IsSuccess = isSuccess;
        Message = message;
        Value = value;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public string Message { get; }
    public T? Value { get; }
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public static ImportExportHistoryResult<T> Success(T value, string message) =>
        new(true, message, value, new Dictionary<string, string[]>());

    public static ImportExportHistoryResult<T> Failure(string message) =>
        new(false, message, default, new Dictionary<string, string[]>());
}
