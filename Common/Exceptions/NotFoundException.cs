using Microsoft.AspNetCore.Http;

namespace TaskManagement.API.Common.Exceptions;

public sealed class NotFoundException : ApiException
{
    public NotFoundException(string message)
        : base(message, StatusCodes.Status404NotFound)
    {
    }
}
