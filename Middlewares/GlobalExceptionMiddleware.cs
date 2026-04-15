using System.Text.Json;
using TaskManagement.API.Common;
using TaskManagement.API.Common.Exceptions;

namespace TaskManagement.API.Middlewares;

public class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "Handled API exception: {Message}", ex.Message);
            await WriteErrorAsync(context, ex.StatusCode, ex.Message, ex.Errors);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            await WriteErrorAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                new[] { "Authentication is required for this action." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                new[] { "An unexpected error occurred." });
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message, IEnumerable<string>? errors)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponseFactory.Error<object?>(message, errors);
        var payload = JsonSerializer.Serialize(response, SerializerOptions);
        await context.Response.WriteAsync(payload);
    }
}
