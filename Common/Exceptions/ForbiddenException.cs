using Microsoft.AspNetCore.Http;

namespace TaskManagement.API.Common.Exceptions;

public sealed class ForbiddenException : ApiException
{
    public ForbiddenException(string message)
        : base(message, StatusCodes.Status403Forbidden)
    {
    }
}
