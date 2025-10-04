namespace Maliev.OrderService.Data.Models;

public class AuditLog
{
    public long AuditId { get; set; }
    public string OrderId { get; set; } = null!;
    public string Action { get; set; } = null!; // OrderCreated, OrderUpdated, StatusChanged, etc.
    public string PerformedBy { get; set; } = null!;
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string? ChangeDetails { get; set; } // JSON: before/after state

    // Navigation Properties
    public Order Order { get; set; } = null!;
}
