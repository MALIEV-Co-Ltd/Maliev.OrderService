using System.Net.Http.Json;

namespace Maliev.OrderService.Api.Services.External;

/// <summary>
/// Client for interacting with the external Notification Service
/// </summary>
public partial class NotificationServiceClient : INotificationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationServiceClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationServiceClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client</param>
    /// <param name="logger">The logger instance</param>
    public NotificationServiceClient(HttpClient httpClient, ILogger<NotificationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> SendOrderNotificationAsync(OrderNotificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/notifications/send", request, cancellationToken);
            response.EnsureSuccessStatusCode();
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
