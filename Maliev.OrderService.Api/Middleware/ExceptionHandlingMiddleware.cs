using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Maliev.OrderService.Api.Middleware;

public partial class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
            DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "The record has been modified by another user. Please refresh and try again."),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Access denied"),
            KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message),
            _ => (HttpStatusCode.InternalServerError, "An internal server error occurred")
        };

        context.Response.StatusCode = (int)statusCode;

        // Log error
        if (statusCode == HttpStatusCode.InternalServerError)
        {
            Log.UnhandledError(_logger, exception);
        }
        else
        {
            Log.HandledError(_logger, exception.GetType().Name, exception.Message, exception);
        }

        var response = new
        {
            error = new
            {
                message,
                type = exception.GetType().Name,
                statusCode = (int)statusCode
            }
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception occurred")]
        public static partial void UnhandledError(ILogger logger, Exception ex);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Handled exception: {ExceptionType} - {Message}")]
        public static partial void HandledError(ILogger logger, string exceptionType, string message, Exception ex);
    }
}
