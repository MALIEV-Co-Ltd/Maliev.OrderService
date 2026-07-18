using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.DTOs.Request
{
    /// <summary>
    /// Request model for updating an existing order
    /// </summary>
    public class UpdateOrderRequest
    {
        /// <summary>Gets or sets the order version for optimistic concurrency control (Base64-encoded RowVersion)</summary>
        [Required(ErrorMessage = "Version (RowVersion) is required for optimistic concurrency")]
        public required string Version { get; set; }

        /// <summary>Gets or sets the order requirements and specifications</summary>
        [MaxLength(5000, ErrorMessage = "Requirements must not exceed 5000 characters")]
        public string? Requirements { get; set; }

        /// <summary>Gets or sets the ordered quantity</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Ordered Quantity must be greater than 0")]
        public int? OrderedQuantity { get; set; }

        /// <summary>Gets or sets the manufactured quantity</summary>
        [Range(0, int.MaxValue, ErrorMessage = "Manufactured Quantity must be greater than or equal to 0")]
        public int? ManufacturedQuantity { get; set; }

        /// <summary>Gets or sets the material ID</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Material ID must be greater than 0")]
        public int? MaterialId { get; set; }

        /// <summary>Gets or sets the color ID</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Color ID must be greater than 0")]
        public int? ColorId { get; set; }

        /// <summary>Gets or sets the surface finishing ID</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Surface Finishing ID must be greater than 0")]
        public int? SurfaceFinishingId { get; set; }

        /// <summary>Gets or sets the lead time in days</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Lead Time Days must be greater than 0")]
        public int? LeadTimeDays { get; set; }

        /// <summary>Gets or sets the promised delivery date</summary>
        public DateTime? PromisedDeliveryDate { get; set; }

        /// <summary>Gets or sets the actual delivery date</summary>
        public DateTime? ActualDeliveryDate { get; set; }

        /// <summary>Gets or sets the quoted amount</summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "Quoted Amount must be greater than 0")]
        public decimal? QuotedAmount { get; set; }

        /// <summary>Gets or sets the quote currency code</summary>
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Quote Currency must be a 3-letter ISO currency code (e.g., THB, USD)")]
        public string? QuoteCurrency { get; set; }

        /// <summary>Gets or sets the employee ID to assign the order to</summary>
        [MaxLength(50, ErrorMessage = "Assigned Employee ID must not exceed 50 characters")]
        public string? AssignedEmployeeId { get; set; }

        /// <summary>Gets or sets the department ID</summary>
        [MaxLength(50, ErrorMessage = "Department ID must not exceed 50 characters")]
        public string? DepartmentId { get; set; }

        /// <summary>Gets or sets the customer purchase order number</summary>
        [MaxLength(100, ErrorMessage = "Customer PO Number must not exceed 100 characters")]
        public string? CustomerPoNumber { get; set; }

        /// <summary>Gets or sets the customer purchase order file ID</summary>
        public Guid? CustomerPoFileId { get; set; }

        /// <summary>Gets or sets the selected billing address ID</summary>
        public Guid? BillingAddressId { get; set; }

        /// <summary>Gets or sets the selected shipping address ID</summary>
        public Guid? ShippingAddressId { get; set; }

        /// <summary>Gets or sets the shipping address line 1 snapshot</summary>
        [MaxLength(500, ErrorMessage = "Shipping Address Line 1 must not exceed 500 characters")]
        public string? ShippingAddressLine1 { get; set; }

        /// <summary>Gets or sets the shipping address line 2 snapshot</summary>
        [MaxLength(500, ErrorMessage = "Shipping Address Line 2 must not exceed 500 characters")]
        public string? ShippingAddressLine2 { get; set; }

        /// <summary>Gets or sets the shipping city snapshot</summary>
        [MaxLength(200, ErrorMessage = "Shipping City must not exceed 200 characters")]
        public string? ShippingCity { get; set; }

        /// <summary>Gets or sets the shipping province snapshot</summary>
        [MaxLength(200, ErrorMessage = "Shipping Province must not exceed 200 characters")]
        public string? ShippingProvince { get; set; }

        /// <summary>Gets or sets the shipping postal code snapshot</summary>
        [MaxLength(20, ErrorMessage = "Shipping Postal Code must not exceed 20 characters")]
        public string? ShippingPostalCode { get; set; }

        /// <summary>Gets or sets the shipping country snapshot</summary>
        [MaxLength(100, ErrorMessage = "Shipping Country must not exceed 100 characters")]
        public string? ShippingCountry { get; set; }

        /// <summary>Gets or sets the legal company name used for billing</summary>
        [MaxLength(200, ErrorMessage = "Billing Company Name must not exceed 200 characters")]
        public string? BillingCompanyName { get; set; }

        /// <summary>Gets or sets the VAT or tax identifier used for billing</summary>
        [MaxLength(50, ErrorMessage = "Billing VAT Number must not exceed 50 characters")]
        public string? BillingVatNumber { get; set; }

        /// <summary>Gets or sets the delivery contact name snapshot</summary>
        [MaxLength(200, ErrorMessage = "Delivery Contact Name must not exceed 200 characters")]
        public string? DeliveryContactName { get; set; }

        /// <summary>Gets or sets the delivery contact phone snapshot</summary>
        [MaxLength(50, ErrorMessage = "Delivery Contact Phone must not exceed 50 characters")]
        public string? DeliveryContactPhone { get; set; }

        /// <summary>Gets or sets the delivery contact email snapshot</summary>
        [MaxLength(200, ErrorMessage = "Delivery Contact Email must not exceed 200 characters")]
        public string? DeliveryContactEmail { get; set; }
    }
}
