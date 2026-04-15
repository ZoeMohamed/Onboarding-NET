using Microsoft.AspNetCore.Http;

namespace TaskManagement.API.Common.Exceptions;

public sealed class BusinessRuleException : ApiException
{
    public BusinessRuleException(string message, IEnumerable<string>? errors = null)
        : base(message, StatusCodes.Status409Conflict, errors)
    {
    }
}
