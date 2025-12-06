using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Maliev.OrderService.Api.Middleware
{
    /// <summary>
    /// Middleware for global exception handling
    /// </summary>
    /// <param name="next">The next delegate in the pipeline</param>
    /// <param name="logger">The logger instance</param>
    public partial class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        /// <summary>
        /// Invokes the middleware
        /// </summary>
        /// <param name="context">The HTTP context</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            (HttpStatusCode statusCode, string message) = exception switch
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
                Log.UnhandledError(logger, exception);
            }
            else if (logger.IsEnabled(LogLevel.Warning))
            {
                string exceptionType = exception.GetType().Name;
                string exceptionMessage = exception.Message;
                Log.HandledError(logger, exceptionType, exceptionMessage, exception);
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

            string json = JsonSerializer.Serialize(response);
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
}
