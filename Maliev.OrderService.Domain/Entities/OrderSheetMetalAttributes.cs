namespace Maliev.OrderService.Domain.Entities
{
    /// <summary>
    /// Represents manufacturing-specific attributes for sheet metal orders.
    /// </summary>
    public class OrderSheetMetalAttributes
    {
        /// <summary>Gets or sets the order identifier (PK and FK).</summary>
        public string OrderId { get; set; } = null!;

        /// <summary>Gets or sets the sheet metal thickness specification.</summary>
        public string? Thickness { get; set; }

        /// <summary>Gets or sets a value indicating whether welding is required.</summary>
        public bool WeldingRequired { get; set; }

        /// <summary>Gets or sets the welding specification details.</summary>
        public string? WeldingDetails { get; set; }

        /// <summary>Gets or sets the dimensional tolerance specification.</summary>
        public string? Tolerance { get; set; }

        /// <summary>Gets or sets the inspection type required.</summary>
        public string? InspectionType { get; set; }

        // Navigation Properties
        /// <summary>Gets or sets the parent order navigation property.</summary>
        public Order Order { get; set; } = null!;
    }
}
