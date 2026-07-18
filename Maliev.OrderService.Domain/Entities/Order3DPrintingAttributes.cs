namespace Maliev.OrderService.Domain.Entities
{
    /// <summary>
    /// Represents manufacturing-specific attributes for 3D printing orders.
    /// </summary>
    public class Order3DPrintingAttributes
    {
        /// <summary>Gets or sets the order identifier (PK and FK).</summary>
        public string OrderId { get; set; } = null!;

        /// <summary>Gets or sets a value indicating whether thread/tap post-processing is required.</summary>
        public bool ThreadTapRequired { get; set; }

        /// <summary>Gets or sets a value indicating whether insert installation is required.</summary>
        public bool InsertRequired { get; set; }

        /// <summary>Gets or sets the part marking text or specification.</summary>
        public string? PartMarking { get; set; }

        /// <summary>Gets or sets a value indicating whether assembly testing is required.</summary>
        public bool PartAssemblyTestRequired { get; set; }

        // Navigation Properties
        /// <summary>Gets or sets the parent order navigation property.</summary>
        public Order Order { get; set; } = null!;
    }
}
