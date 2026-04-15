using Microsoft.AspNetCore.Http;

namespace TaskManagement.API.Common.Exceptions;

public sealed class ValidationException : ApiException
{
    public ValidationException(string message, IEnumerable<string>? errors = null)
        : base(message, StatusCodes.Status400BadRequest, errors)
    {
    }
}
