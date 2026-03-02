namespace Maliev.OrderService.Domain.Entities
{
    /// <summary>
    /// Represents a customer's notification subscription preferences.
    /// </summary>
    public class NotificationSubscription
    {
        /// <summary>Gets or sets the subscription identifier.</summary>
        public int SubscriptionId { get; set; }

        /// <summary>Gets or sets the customer identifier.</summary>
        public string CustomerId { get; set; } = null!;

        /// <summary>Gets or sets a value indicating whether the customer is subscribed to notifications.</summary>
        public bool IsSubscribed { get; set; } = true;

        /// <summary>Gets or sets the JSON array of notification channels (e.g., ["LINE", "Email"]).</summary>
        public string Channels { get; set; } = "[]";

        /// <summary>Gets or sets when the subscription was last updated.</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
