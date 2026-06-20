using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Application.DTOs.Request
{
    /// <summary>
    /// Request model for creating a new order.
    /// </summary>
    public class CreateOrderRequest
    {
        /// <summary>Gets or sets the customer unique identifier.</summary>
        [Required(ErrorMessage = "Customer ID is required")]
        [MaxLength(50, ErrorMessage = "Customer ID must not exceed 50 characters")]
        public required string CustomerId { get; set; }

        /// <summary>Gets or sets the customer type (Customer or Employee).</summary>
        [Required(ErrorMessage = "Customer Type is required")]
        [RegularExpression("^(Customer|Employee)$", ErrorMessage = "Customer Type must be 'Customer' or 'Employee'")]
        public required string CustomerType { get; set; }

        /// <summary>Gets or sets the service category ID.</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Service Category ID must be greater than 0")]
        public required int ServiceCategoryId { get; set; }

        /// <summary>Gets or sets the process type ID.</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Process Type ID must be greater than 0")]
        public int? ProcessTypeId { get; set; }

        /// <summary>Gets or sets the order requirements and specifications.</summary>
        [MaxLength(5000, ErrorMessage = "Requirements must not exceed 5000 characters")]
        public string? Requirements { get; set; }

        /// <summary>Gets or sets whether this order should be marked as confidential.</summary>
        public bool IsConfidential { get; set; }

        /// <summary>Gets or sets the ordered quantity.</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Ordered Quantity must be greater than 0")]
        public int? OrderedQuantity { get; set; }

        /// <summary>Gets or sets the material ID.</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Material ID must be greater than 0")]
        public int? MaterialId { get; set; }

        /// <summary>Gets or sets the color ID.</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Color ID must be greater than 0")]
        public int? ColorId { get; set; }

        /// <summary>Gets or sets the surface finishing ID.</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Surface Finishing ID must be greater than 0")]
        public int? SurfaceFinishingId { get; set; }

        /// <summary>Gets or sets the lead time in days.</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Lead Time Days must be greater than 0")]
        public int? LeadTimeDays { get; set; }

        /// <summary>Gets or sets the promised delivery date.</summary>
        public DateTime? PromisedDeliveryDate { get; set; }

        /// <summary>Gets or sets the employee ID to assign the order to.</summary>
        [MaxLength(50, ErrorMessage = "Assigned Employee ID must not exceed 50 characters")]
        public string? AssignedEmployeeId { get; set; }

        /// <summary>Gets or sets the department ID.</summary>
        [MaxLength(50, ErrorMessage = "Department ID must not exceed 50 characters")]
        public string? DepartmentId { get; set; }

        /// <summary>Gets or sets the customer purchase order number.</summary>
        [MaxLength(100, ErrorMessage = "Customer PO Number must not exceed 100 characters")]
        public string? CustomerPoNumber { get; set; }

        /// <summary>Gets or sets the customer purchase order file ID.</summary>
        public Guid? CustomerPoFileId { get; set; }

        /// <summary>Gets or sets the formal quotation identifier accepted for this order.</summary>
        public Guid? QuoteId { get; set; }

        /// <summary>Gets or sets the formal quotation number accepted for this order.</summary>
        [MaxLength(80)]
        public string? QuoteNumber { get; set; }

        /// <summary>Gets or sets the immutable quotation version identifier accepted for this order.</summary>
        public Guid? QuoteVersionId { get; set; }

        /// <summary>Gets or sets the immutable quotation version number accepted for this order.</summary>
        [Range(1, int.MaxValue)]
        public int? QuoteVersionNumber { get; set; }
    }
}
