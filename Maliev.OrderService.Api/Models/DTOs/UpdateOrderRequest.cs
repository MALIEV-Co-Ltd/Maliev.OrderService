using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.Models.DTOs
{
    /// <summary>
    /// Represents a request to update an existing order.
    /// </summary>
    public class UpdateOrderRequest
    {
        /// <summary>
        /// Gets or sets the ID of the order to update. This field is required.
        /// </summary>
        [Required]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the new name of the order. This field is required.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the new description of the order.
        /// </summary>
        [StringLength(250)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the new quantity of the order. This field is required.
        /// </summary>
        [Required]
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the new unit price of the order. This field is required.
        /// </summary>
        [Required]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Gets or sets the new discount percentage of the order. This field is required.
        /// </summary>
        [Required]
        public decimal DiscountPercent { get; set; }

        /// <summary>
        /// Gets or sets the new manufactured quantity of the order. This field is required.
        /// </summary>
        [Required]
        public int Manufactured { get; set; }

        /// <summary>
        /// Gets or sets the new promised date of the order.
        /// </summary>
        public DateTime? PromisedDate { get; set; }

        /// <summary>
        /// Gets or sets the new finished date of the order.
        /// </summary>
        public DateTime? FinishedDate { get; set; }

        /// <summary>
        /// Gets or sets the new tracking number of the order.
        /// </summary>
        [StringLength(250)]
        public string? TrackingNumber { get; set; }

        /// <summary>
        /// Gets or sets the new process ID associated with the order. This field is required.
        /// </summary>
        [Required]
        public int ProcessId { get; set; }

        /// <summary>
        /// Gets or sets the new color ID associated with the order.
        /// </summary>
        public int? ColorId { get; set; }

        /// <summary>
        /// Gets or sets the new currency ID associated with the order.
        /// </summary>
        public int? CurrencyId { get; set; }

        /// <summary>
        /// Gets or sets the new customer ID associated with the order.
        /// </summary>
        public int? CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the new employee ID associated with the order.
        /// </summary>
        public int? EmployeeId { get; set; }

        /// <summary>
        /// Gets or sets the new material ID associated with the order.
        /// </summary>
        public int? MaterialId { get; set; }

        /// <summary>
        /// Gets or sets the new surface finish ID associated with the order.
        /// </summary>
        public int? SurfaceFinishId { get; set; }
    }
}