namespace TaskManagement.API.Common.Exceptions;

public class ApiException : Exception
{
    public int StatusCode { get; }
    public List<string> Errors { get; }

    public ApiException(string message, int statusCode, IEnumerable<string>? errors = null) : base(message)
    {
        StatusCode = statusCode;
        Errors = errors?.ToList() ?? new List<string>();
    }
}
