namespace Maliev.OrderService.Application.DTOs.Response
{
    /// <summary>
    /// Response model for an order status history entry.
    /// </summary>
    public class OrderStatusResponse
    {
        /// <summary>Gets or sets the status identifier.</summary>
        public long StatusId { get; set; }

        /// <summary>Gets or sets the order identifier.</summary>
        public required string OrderId { get; set; }

        /// <summary>Gets or sets the status value.</summary>
        public required string Status { get; set; }

        /// <summary>Gets or sets internal notes (employee-only).</summary>
        public string? InternalNotes { get; set; }

        /// <summary>Gets or sets customer-visible notes.</summary>
        public string? CustomerNotes { get; set; }

        /// <summary>Gets or sets who updated this status.</summary>
        public required string UpdatedBy { get; set; }

        /// <summary>Gets or sets when this status was recorded.</summary>
        public DateTime Timestamp { get; set; }
    }
}
