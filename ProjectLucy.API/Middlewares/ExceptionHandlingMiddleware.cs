using System.Text.Json;
using ProjectLucy.Application.Common.Exceptions;
using ProjectLucy.Domain.Exceptions;

namespace ProjectLucy.API.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            ValidationAppException ex => (ex.StatusCode, ex.Message, ex.Errors),
            AppException ex => (ex.StatusCode, ex.Message, ex.Errors),
            DomainException ex => (400, ex.Message, (IReadOnlyCollection<string>?)null),
            _ => (500, "An unexpected error occurred", (IReadOnlyCollection<string>?)null)
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Request failed with server error");
        }
        else
        {
            _logger.LogWarning(exception, "Request failed with handled exception");
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = statusCode,
            message = message,
            data = (object?)null,
            errors
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
