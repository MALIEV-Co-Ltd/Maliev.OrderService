namespace Maliev.OrderService.Domain.Entities
{
    /// <summary>
    /// Represents a note or comment associated with an order.
    /// </summary>
    public class OrderNote
    {
        /// <summary>Gets or sets the note identifier.</summary>
        public long NoteId { get; set; }

        /// <summary>Gets or sets the order identifier.</summary>
        public string OrderId { get; set; } = null!;

        /// <summary>Gets or sets the note type (customer or internal).</summary>
        public string NoteType { get; set; } = null!;

        /// <summary>Gets or sets the note text content.</summary>
        public string NoteText { get; set; } = null!;

        /// <summary>Gets or sets who created the note.</summary>
        public string CreatedBy { get; set; } = null!;

        /// <summary>Gets or sets when the note was created.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        /// <summary>Gets or sets the parent order navigation property.</summary>
        public Order Order { get; set; } = null!;
    }
}
