using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.DTOs.Request
{
    /// <summary>
    /// Request model for creating a new order
    /// </summary>
    public class CreateOrderRequest
    {
        /// <summary>Gets or sets the customer unique identifier</summary>
        [Required(ErrorMessage = "Customer ID is required")]
        [MaxLength(50, ErrorMessage = "Customer ID must not exceed 50 characters")]
        public required string CustomerId { get; set; }

        /// <summary>Gets or sets the customer type (Customer or Employee)</summary>
        [Required(ErrorMessage = "Customer Type is required")]
        [RegularExpression("^(Customer|Employee)$", ErrorMessage = "Customer Type must be 'Customer' or 'Employee'")]
        public required string CustomerType { get; set; }

        /// <summary>Gets or sets the service category ID</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Service Category ID must be greater than 0")]
        public required int ServiceCategoryId { get; set; }

        /// <summary>Gets or sets the process type ID</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Process Type ID must be greater than 0")]
        public int? ProcessTypeId { get; set; }

        /// <summary>Gets or sets the order requirements and specifications</summary>
        [MaxLength(5000, ErrorMessage = "Requirements must not exceed 5000 characters")]
        public string? Requirements { get; set; }

        /// <summary>Gets or sets whether this order should be marked as confidential</summary>
        public bool IsConfidential { get; set; }

        /// <summary>Gets or sets the ordered quantity</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Ordered Quantity must be greater than 0")]
        public int? OrderedQuantity { get; set; }

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

        /// <summary>Gets or sets the quoted order amount.</summary>
        [Range(0, 999_999_999.99)]
        public decimal? QuotedAmount { get; set; }

        /// <summary>Gets or sets the quote currency code.</summary>
        [StringLength(3, MinimumLength = 3)]
        public string? QuoteCurrency { get; set; }

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

        /// <summary>Gets or sets structured production item snapshots for job creation.</summary>
        public List<CreateOrderProductionItemRequest> ProductionItems { get; set; } = [];
    }

    /// <summary>
    /// Structured production item supplied when creating an order from a configured quote.
    /// </summary>
    public class CreateOrderProductionItemRequest
    {
        /// <summary>Gets or sets the source project or quote identifier.</summary>
        public Guid? SourceProjectId { get; set; }

        /// <summary>Gets or sets the source part identifier.</summary>
        public Guid? SourceProjectPartId { get; set; }

        /// <summary>Gets or sets the resolved material identifier used by production.</summary>
        public required Guid MaterialId { get; set; }

        /// <summary>Gets or sets the locked material snapshot JSON.</summary>
        [Required]
        public required string MaterialSnapshotJson { get; set; }

        /// <summary>Gets or sets the locked configuration snapshot JSON.</summary>
        [Required]
        public required string ConfigurationSnapshotJson { get; set; }

        /// <summary>Gets or sets the manufacturing technology.</summary>
        [Required]
        [MaxLength(80)]
        public required string Technology { get; set; }

        /// <summary>Gets or sets the part volume in cubic centimeters.</summary>
        [Range(0, double.MaxValue)]
        public decimal VolumeCm3 { get; set; }

        /// <summary>Gets or sets the ordered quantity for this item.</summary>
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        /// <summary>Gets or sets the estimated print time in minutes.</summary>
        [Range(0, int.MaxValue)]
        public int EstimatedPrintTimeMinutes { get; set; }

        /// <summary>Gets or sets the promised delivery date for this item.</summary>
        public DateTime? DeliveryDate { get; set; }
    }
}
