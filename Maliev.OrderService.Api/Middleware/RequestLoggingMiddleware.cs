using System.Diagnostics;

namespace Maliev.OrderService.Api.Middleware;

public partial class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;

        try
        {
            await _next(context);
            stopwatch.Stop();

            Log.RequestCompleted(
                _logger,
                requestMethod,
                requestPath,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Log.RequestFailed(
                _logger,
                requestMethod,
                requestPath,
                stopwatch.ElapsedMilliseconds,
                ex);

            throw;
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "{Method} {Path} responded {StatusCode} in {ElapsedMs}ms")]
        public static partial void RequestCompleted(
            ILogger logger,
            string method,
            string path,
            int statusCode,
            long elapsedMs);

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "{Method} {Path} failed after {ElapsedMs}ms")]
        public static partial void RequestFailed(
            ILogger logger,
            string method,
            string path,
            long elapsedMs,
            Exception ex);
    }
}
