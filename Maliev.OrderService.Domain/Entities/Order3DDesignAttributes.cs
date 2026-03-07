namespace Maliev.OrderService.Domain.Entities
{
    /// <summary>
    /// Represents service-specific attributes for 3D design orders.
    /// </summary>
    public class Order3DDesignAttributes
    {
        /// <summary>Gets or sets the order identifier (PK and FK).</summary>
        public string OrderId { get; set; } = null!;

        /// <summary>Gets or sets the design complexity level (Simple, Medium, Complex).</summary>
        public string? ComplexityLevel { get; set; }

        /// <summary>Gets or sets the deliverables as a CSV list.</summary>
        public string? Deliverables { get; set; }

        /// <summary>Gets or sets the design software to be used.</summary>
        public string? DesignSoftware { get; set; }

        /// <summary>Gets or sets the number of revision rounds included.</summary>
        public int RevisionRounds { get; set; } = 2;

        // Navigation Properties
        /// <summary>Gets or sets the parent order navigation property.</summary>
        public Order Order { get; set; } = null!;
    }
}
