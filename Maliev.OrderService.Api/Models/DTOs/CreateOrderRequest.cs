using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.Models.DTOs
{
    /// <summary>
    /// Represents a request to create a new order.
    /// </summary>
    public class CreateOrderRequest
    {
        /// <summary>
        /// Gets or sets the name of the order. This field is required.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the description of the order.
        /// </summary>
        [StringLength(250)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the quantity of the order. This field is required.
        /// </summary>
        [Required]
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the unit price of the order. This field is required.
        /// </summary>
        [Required]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Gets or sets the discount percentage of the order. This field is required.
        /// </summary>
        [Required]
        public decimal DiscountPercent { get; set; }

        /// <summary>
        /// Gets or sets the manufactured quantity of the order. This field is required.
        /// </summary>
        [Required]
        public int Manufactured { get; set; }

        /// <summary>
        /// Gets or sets the promised date of the order.
        /// </summary>
        public DateTime? PromisedDate { get; set; }

        /// <summary>
        /// Gets or sets the finished date of the order.
        /// </summary>
        public DateTime? FinishedDate { get; set; }

        /// <summary>
        /// Gets or sets the tracking number of the order.
        /// </summary>
        [StringLength(250)]
        public string? TrackingNumber { get; set; }

        /// <summary>
        /// Gets or sets the process ID associated with the order. This field is required.
        /// </summary>
        [Required]
        public int ProcessId { get; set; }

        /// <summary>
        /// Gets or sets the color ID associated with the order.
        /// </summary>
        public int? ColorId { get; set; }

        /// <summary>
        /// Gets or sets the currency ID associated with the order.
        /// </summary>
        public int? CurrencyId { get; set; }

        /// <summary>
        /// Gets or sets the customer ID associated with the order.
        /// </summary>
        public int? CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the employee ID associated with the order.
        /// </summary>
        public int? EmployeeId { get; set; }

        /// <summary>
        /// Gets or sets the material ID associated with the order.
        /// </summary>
        public int? MaterialId { get; set; }

        /// <summary>
        /// Gets or sets the surface finish ID associated with the order.
        /// </summary>
        public int? SurfaceFinishId { get; set; }
    }
}