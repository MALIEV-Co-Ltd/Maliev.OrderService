using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.DTOs.Request
{
    /// <summary>
    /// Request model for updating the outsourcing status of an order.
    /// Employee-only operation.
    /// </summary>
    public class UpdateOutsourcingRequest
    {
        /// <summary>Gets or sets whether the order is outsourced.</summary>
        [Required]
        public required bool IsOutsourced { get; set; }

        /// <summary>Gets or sets the supplier cost in THB. Required when isOutsourced is true.</summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "Supplier cost must be greater than 0")]
        public decimal? SupplierCostTHB { get; set; }

        /// <summary>Gets or sets the supplier name. Required when isOutsourced is true.</summary>
        [MaxLength(200, ErrorMessage = "Supplier name must not exceed 200 characters")]
        public string? SupplierName { get; set; }

        /// <summary>Gets or sets the estimated delivery date from supplier.</summary>
        public DateTime? SupplierEstimatedDelivery { get; set; }
    }
}
