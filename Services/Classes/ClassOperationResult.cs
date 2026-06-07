namespace EduBridge.Services.Classes;

public sealed class ClassOperationResult<T>
{
    private ClassOperationResult(
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

    public static ClassOperationResult<T> Success(T value, string message) =>
        new(true, message, value, new Dictionary<string, string[]>());

    public static ClassOperationResult<T> Failure(string message) =>
        new(false, message, default, new Dictionary<string, string[]>());

    public static ClassOperationResult<T> Failure(
        string message,
        IReadOnlyDictionary<string, string[]> errors) =>
        new(false, message, default, errors);
}
