namespace Maliev.OrderService.Domain.Entities
{
    /// <summary>
    /// Represents a manufacturing process type within a service category (e.g., FDM, SLA, CNC).
    /// </summary>
    public class ProcessType
    {
        /// <summary>Gets or sets the process type identifier.</summary>
        public int ProcessTypeId { get; set; }

        /// <summary>Gets or sets the parent service category identifier.</summary>
        public int ServiceCategoryId { get; set; }

        /// <summary>Gets or sets the process type name.</summary>
        public string Name { get; set; } = null!;

        /// <summary>Gets or sets the process type description.</summary>
        public string? Description { get; set; }

        /// <summary>Gets or sets a value indicating whether this process type is active.</summary>
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        /// <summary>Gets or sets the parent service category navigation property.</summary>
        public ServiceCategory ServiceCategory { get; set; } = null!;

        /// <summary>Gets or sets the collection of orders using this process type.</summary>
        public ICollection<Order> Orders { get; set; } = [];
    }
}
