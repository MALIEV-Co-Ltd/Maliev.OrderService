namespace Maliev.OrderService.Api.Services.External
{
    /// <summary>
    /// Client for interacting with the external Notification Service
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="NotificationServiceClient"/> class.
    /// </remarks>
    /// <param name="httpClient">The HTTP client</param>
    /// <param name="logger">The logger instance</param>
    public partial class NotificationServiceClient(HttpClient httpClient, ILogger<NotificationServiceClient> logger) : INotificationServiceClient
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<NotificationServiceClient> _logger = logger;

        /// <inheritdoc />
        public async Task<bool> SendOrderNotificationAsync(OrderNotificationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/api/v1/notifications/send", request, cancellationToken);
                _ = response.EnsureSuccessStatusCode();
                return true;
            }
            catch (HttpRequestException ex)
            {
                Log.FailedToSendNotification(_logger, request.OrderId, request.NotificationType, ex);
                return false;
            }
        }

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send {NotificationType} notification for order {OrderId}")]
            public static partial void FailedToSendNotification(ILogger logger, string orderId, string notificationType, Exception ex);
        }
    }
}
