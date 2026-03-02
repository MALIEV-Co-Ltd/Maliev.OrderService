namespace Maliev.OrderService.Domain.Entities
{
    /// <summary>
    /// Represents a manufacturing service category (e.g., 3D Printing, CNC Machining).
    /// </summary>
    public class ServiceCategory
    {
        /// <summary>Gets or sets the category identifier.</summary>
        public int CategoryId { get; set; }

        /// <summary>Gets or sets the category name.</summary>
        public string Name { get; set; } = null!;

        /// <summary>Gets or sets the category description.</summary>
        public string? Description { get; set; }

        /// <summary>Gets or sets a value indicating whether this category is active.</summary>
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        /// <summary>Gets or sets the collection of process types under this category.</summary>
        public ICollection<ProcessType> ProcessTypes { get; set; } = [];

        /// <summary>Gets or sets the collection of orders in this category.</summary>
        public ICollection<Order> Orders { get; set; } = [];
    }
}
