namespace Maliev.OrderService.Domain.Entities
{
    /// <summary>
    /// Represents an audit log entry for order changes.
    /// </summary>
    public class AuditLog
    {
        /// <summary>Gets or sets the audit log identifier.</summary>
        public long AuditId { get; set; }

        /// <summary>Gets or sets the order identifier.</summary>
        public string OrderId { get; set; } = null!;

        /// <summary>Gets or sets the action performed (OrderCreated, OrderUpdated, StatusChanged, etc.).</summary>
        public string Action { get; set; } = null!;

        /// <summary>Gets or sets who performed the action.</summary>
        public string PerformedBy { get; set; } = null!;

        /// <summary>Gets or sets when the action was performed.</summary>
        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Gets or sets the type of entity that was changed.</summary>
        public string EntityType { get; set; } = null!;

        /// <summary>Gets or sets the identifier of the changed entity.</summary>
        public string EntityId { get; set; } = null!;

        /// <summary>Gets or sets the JSON-formatted before/after change details.</summary>
        public string? ChangeDetails { get; set; }

        // Navigation Properties
        /// <summary>Gets or sets the parent order navigation property.</summary>
        public Order Order { get; set; } = null!;
    }
}
