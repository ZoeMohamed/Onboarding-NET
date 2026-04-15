namespace TaskManagement.API.Common;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public List<string> Errors { get; init; } = new();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public static class ApiResponseFactory
{
    public static ApiResponse<T> Success<T>(T data, string message = "Success") => new()
    {
        Success = true,
        Message = message,
        Data = data,
        Errors = new List<string>()
    };

    public static ApiResponse<T> Error<T>(string message, IEnumerable<string>? errors = null) => new()
    {
        Success = false,
        Message = message,
        Data = default,
        Errors = errors?.ToList() ?? new List<string>()
    };
}
