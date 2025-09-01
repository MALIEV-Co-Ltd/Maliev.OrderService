using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.Models.DTOs
{
    /// <summary>
    /// Represents an order data transfer object.
    /// </summary>
    public class OrderDto
    {
        /// <summary>
        /// Gets or sets the ID of the order.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the order.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the description of the order.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the quantity of the order.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the unit price of the order.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Gets or sets the discount percentage of the order.
        /// </summary>
        public decimal DiscountPercent { get; set; }

        /// <summary>
        /// Gets or sets the manufactured quantity of the order.
        /// </summary>
        public int Manufactured { get; set; }

        /// <summary>
        /// Gets or sets the subtotal of the order.
        /// </summary>
        public decimal Subtotal { get; set; }

        /// <summary>
        /// Gets or sets the remaining quantity of the order.
        /// </summary>
        public int Remaining { get; set; }

        /// <summary>
        /// Gets or sets the creation date of the order.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the last modification date of the order.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the promised date of the order.
        /// </summary>
        public DateTime? PromisedDate { get; set; }

        /// <summary>
        /// Gets or sets the finished date of the order.
        /// </summary>
        public DateTime? FinishedDate { get; set; }

        /// <summary>
        /// Gets or sets the turnaround time in days.
        /// </summary>
        public int? Turnaround { get; set; }

        /// <summary>
        /// Gets or sets the tracking number of the order.
        /// </summary>
        public string? TrackingNumber { get; set; }

        /// <summary>
        /// Gets or sets the process ID associated with the order.
        /// </summary>
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