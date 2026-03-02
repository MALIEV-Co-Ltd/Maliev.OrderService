namespace Maliev.OrderService.Domain.Entities
{
    /// <summary>
    /// Represents manufacturing-specific attributes for CNC machining orders.
    /// </summary>
    public class OrderCncMachiningAttributes
    {
        /// <summary>Gets or sets the order identifier (PK and FK).</summary>
        public string OrderId { get; set; } = null!;

        /// <summary>Gets or sets a value indicating whether tapping is required.</summary>
        public bool TapRequired { get; set; }

        /// <summary>Gets or sets the dimensional tolerance specification.</summary>
        public string? Tolerance { get; set; }

        /// <summary>Gets or sets the surface roughness requirement (Ra value).</summary>
        public string? SurfaceRoughness { get; set; }

        /// <summary>Gets or sets the inspection type required (e.g., CMM, visual).</summary>
        public string? InspectionType { get; set; }

        // Navigation Properties
        /// <summary>Gets or sets the parent order navigation property.</summary>
        public Order Order { get; set; } = null!;
    }
}
