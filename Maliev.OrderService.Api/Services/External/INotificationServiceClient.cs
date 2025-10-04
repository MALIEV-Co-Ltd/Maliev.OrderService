namespace Maliev.OrderService.Api.Services.External;

public interface INotificationServiceClient
{
    Task<bool> SendOrderNotificationAsync(OrderNotificationRequest request, CancellationToken cancellationToken = default);
}

public class OrderNotificationRequest
{
    public required string OrderId { get; set; }
    public required string CustomerId { get; set; }
    public required string NotificationType { get; set; } // StatusChange, FileUploaded, OrderCancelled, etc.
    public required string Message { get; set; }
    public List<string> Channels { get; set; } = new(); // LINE, Email, SMS
}
