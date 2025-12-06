namespace Maliev.OrderService.Api.Services.External;

/// <summary>
/// Interface for interacting with the external Notification Service
/// </summary>
public interface INotificationServiceClient
{
    /// <summary>
    /// Sends a notification related to an order
    /// </summary>
    /// <param name="request">The notification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> SendOrderNotificationAsync(OrderNotificationRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for sending an order notification
/// </summary>
public class OrderNotificationRequest
{
    /// <summary>Gets or sets the order ID</summary>
    public required string OrderId { get; set; }
    
    /// <summary>Gets or sets the customer ID</summary>
    public required string CustomerId { get; set; }
    
    /// <summary>Gets or sets the notification type (e.g., StatusChange, FileUploaded)</summary>
    public required string NotificationType { get; set; }
    
    /// <summary>Gets or sets the notification message</summary>
    public required string Message { get; set; }
    
    /// <summary>Gets or sets the notification channels (e.g., LINE, Email, SMS)</summary>
    public List<string> Channels { get; set; } = new();
}
