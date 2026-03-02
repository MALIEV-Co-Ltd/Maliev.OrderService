namespace Maliev.OrderService.Domain.Entities
{
    /// <summary>
    /// Represents an order status history entry.
    /// </summary>
    public class OrderStatus
    {
        /// <summary>Gets or sets the status identifier.</summary>
        public long StatusId { get; set; }

        /// <summary>Gets or sets the order identifier.</summary>
        public string OrderId { get; set; } = null!;

        /// <summary>Gets or sets the status value (New, Reviewing, Rejected, etc.).</summary>
        public string Status { get; set; } = null!;

        /// <summary>Gets or sets internal notes visible to employees only (encrypted at rest).</summary>
        public string? InternalNotes { get; set; }

        /// <summary>Gets or sets customer-visible notes.</summary>
        public string? CustomerNotes { get; set; }

        /// <summary>Gets or sets when this status entry was created.</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Gets or sets who updated this status.</summary>
        public string UpdatedBy { get; set; } = null!;

        // Navigation Properties
        /// <summary>Gets or sets the parent order navigation property.</summary>
        public Order Order { get; set; } = null!;
    }
}
