namespace Maliev.OrderService.Data.Models;

public class OrderStatus
{
    public long StatusId { get; set; }
    public string OrderId { get; set; } = null!;
    public string Status { get; set; } = null!; // Enum: New, Reviewing, Rejected, etc.
    public string? InternalNotes { get; set; } // Encrypted at rest, employee-only
    public string? CustomerNotes { get; set; } // Customer-visible notes
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = null!;

    // Navigation Properties
    public Order Order { get; set; } = null!;
}
