using System.Diagnostics;

namespace Maliev.OrderService.Api.Middleware
{
    /// <summary>
    /// Middleware for logging HTTP requests and responses
    /// </summary>
    /// <param name="next">The next delegate in the pipeline</param>
    /// <param name="logger">The logger instance</param>
    public partial class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        /// <summary>
        /// Invokes the middleware
        /// </summary>
        /// <param name="context">The HTTP context</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            PathString requestPath = context.Request.Path;
            string requestMethod = context.Request.Method;

            try
            {
                await next(context);
                stopwatch.Stop();

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    int statusCode = context.Response.StatusCode;
                    long elapsedMs = stopwatch.ElapsedMilliseconds;
                    string path = requestPath.Value ?? string.Empty;

                    Log.RequestCompleted(
                        logger,
                        requestMethod,
                        path,
                        statusCode,
                        elapsedMs);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                if (logger.IsEnabled(LogLevel.Error))
                {
                    long elapsedMs = stopwatch.ElapsedMilliseconds;
                    string path = requestPath.Value ?? string.Empty;

                    Log.RequestFailed(
                        logger,
                        requestMethod,
                        path,
                        elapsedMs,
                        ex);
                }

                throw;
            }
        }

        private static partial class Log
        {
            [LoggerMessage(
                Level = LogLevel.Debug,
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
}
