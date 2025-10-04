namespace Maliev.OrderService.Data.Models;

public class NotificationSubscription
{
    public int SubscriptionId { get; set; }
    public string CustomerId { get; set; } = null!;
    public bool IsSubscribed { get; set; } = true;
    public string Channels { get; set; } = "[]"; // JSON array: ["LINE", "Email"]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
