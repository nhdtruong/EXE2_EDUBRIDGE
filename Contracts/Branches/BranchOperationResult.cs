using System.Collections.Generic;

namespace EduBridge.Contracts.Branches;

public class BranchOperationResult<T>
{
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public T? Data { get; private set; }
    public IReadOnlyDictionary<string, string[]> Errors { get; private set; }

    private BranchOperationResult(bool isSuccess, string message, T? data, IReadOnlyDictionary<string, string[]> errors)
    {
        IsSuccess = isSuccess;
        Message = message;
        Data = data;
        Errors = errors;
    }

    public static BranchOperationResult<T> Success(T data, string message = "Success") =>
        new(true, message, data, new Dictionary<string, string[]>());

    public static BranchOperationResult<T> Failure(string message, IReadOnlyDictionary<string, string[]> errors) =>
        new(false, message, default, errors);
}
