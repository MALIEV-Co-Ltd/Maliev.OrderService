namespace Maliev.OrderService.Api.DTOs.Response
{
    /// <summary>
    /// Response model for order details
    /// </summary>
    public class OrderResponse
    {
        /// <summary>Gets or sets the order unique identifier</summary>
        public required string OrderId { get; set; }

        /// <summary>Gets or sets the customer unique identifier</summary>
        public required string CustomerId { get; set; }

        /// <summary>Gets or sets the customer type (Business/Individual)</summary>
        public required string CustomerType { get; set; }

        /// <summary>Gets or sets the service category ID</summary>
        public required int ServiceCategoryId { get; set; }

        /// <summary>Gets or sets the service category name</summary>
        public string? ServiceCategoryName { get; set; }

        /// <summary>Gets or sets the process type ID</summary>
        public int? ProcessTypeId { get; set; }

        /// <summary>Gets or sets the process type name</summary>
        public string? ProcessTypeName { get; set; }

        /// <summary>Gets or sets the current order status (derived from latest OrderStatus)</summary>
        public string? CurrentStatus { get; set; }

        /// <summary>Gets or sets the order requirements and specifications</summary>
        public string? Requirements { get; set; }

        /// <summary>Gets or sets whether this order is marked as confidential</summary>
        public bool IsConfidential { get; set; }

        /// <summary>Gets or sets the ordered quantity</summary>
        public int? OrderedQuantity { get; set; }

        /// <summary>Gets or sets the manufactured quantity</summary>
        public int? ManufacturedQuantity { get; set; }

        /// <summary>Gets or sets the material ID</summary>
        public int? MaterialId { get; set; }

        /// <summary>Gets or sets the color ID</summary>
        public int? ColorId { get; set; }

        /// <summary>Gets or sets the surface finishing ID</summary>
        public int? SurfaceFinishingId { get; set; }

        /// <summary>Gets or sets the material name (cached)</summary>
        public string? MaterialName { get; set; }

        /// <summary>Gets or sets the color name (cached)</summary>
        public string? ColorName { get; set; }

        /// <summary>Gets or sets the surface finishing name (cached)</summary>
        public string? SurfaceFinishingName { get; set; }

        /// <summary>Gets or sets when the material cache was last updated</summary>
        public DateTime? MaterialCacheUpdatedAt { get; set; }

        /// <summary>Gets or sets the lead time in days</summary>
        public int? LeadTimeDays { get; set; }

        /// <summary>Gets or sets the promised delivery date</summary>
        public DateTime? PromisedDeliveryDate { get; set; }

        /// <summary>Gets or sets the actual delivery date</summary>
        public DateTime? ActualDeliveryDate { get; set; }

        /// <summary>Gets or sets the quoted amount</summary>
        public decimal? QuotedAmount { get; set; }

        /// <summary>Gets or sets the quote currency code</summary>
        public string? QuoteCurrency { get; set; }

        /// <summary>Gets or sets the formal quotation identifier accepted for this order</summary>
        public Guid? QuoteId { get; set; }

        /// <summary>Gets or sets the formal quotation number accepted for this order</summary>
        public string? QuoteNumber { get; set; }

        /// <summary>Gets or sets the immutable quotation version identifier accepted for this order</summary>
        public Guid? QuoteVersionId { get; set; }

        /// <summary>Gets or sets the immutable quotation version number accepted for this order</summary>
        public int? QuoteVersionNumber { get; set; }

        /// <summary>Gets or sets the customer purchase order number</summary>
        public string? CustomerPoNumber { get; set; }

        /// <summary>Gets or sets the customer purchase order file ID</summary>
        public Guid? CustomerPoFileId { get; set; }

        /// <summary>Gets or sets the selected billing address ID</summary>
        public Guid? BillingAddressId { get; set; }

        /// <summary>Gets or sets the selected shipping address ID</summary>
        public Guid? ShippingAddressId { get; set; }

        /// <summary>Gets or sets the shipping address line 1 snapshot</summary>
        public string? ShippingAddressLine1 { get; set; }

        /// <summary>Gets or sets the shipping address line 2 snapshot</summary>
        public string? ShippingAddressLine2 { get; set; }

        /// <summary>Gets or sets the shipping city snapshot</summary>
        public string? ShippingCity { get; set; }

        /// <summary>Gets or sets the shipping province snapshot</summary>
        public string? ShippingProvince { get; set; }

        /// <summary>Gets or sets the shipping postal code snapshot</summary>
        public string? ShippingPostalCode { get; set; }

        /// <summary>Gets or sets the shipping country snapshot</summary>
        public string? ShippingCountry { get; set; }

        /// <summary>Gets or sets the legal company name used for billing</summary>
        public string? BillingCompanyName { get; set; }

        /// <summary>Gets or sets the VAT or tax identifier used for billing</summary>
        public string? BillingVatNumber { get; set; }

        /// <summary>Gets or sets the delivery contact name snapshot</summary>
        public string? DeliveryContactName { get; set; }

        /// <summary>Gets or sets the delivery contact phone snapshot</summary>
        public string? DeliveryContactPhone { get; set; }

        /// <summary>Gets or sets the delivery contact email snapshot</summary>
        public string? DeliveryContactEmail { get; set; }

        /// <summary>Gets or sets the payment transaction ID</summary>
        public string? PaymentId { get; set; }

        /// <summary>Gets or sets the payment status</summary>
        public string PaymentStatus { get; set; } = "Unpaid";

        /// <summary>Gets or sets the assigned employee ID</summary>
        public string? AssignedEmployeeId { get; set; }

        /// <summary>Gets or sets the department ID</summary>
        public string? DepartmentId { get; set; }

        /// <summary>Gets or sets whether this order is outsourced to an external supplier</summary>
        public bool IsOutsourced { get; set; }

        /// <summary>Gets or sets the supplier cost in THB (null for in-house orders)</summary>
        public decimal? SupplierCostTHB { get; set; }

        /// <summary>Gets or sets the supplier name (null for in-house orders)</summary>
        public string? SupplierName { get; set; }

        /// <summary>Gets or sets the estimated delivery date from the supplier</summary>
        public DateTime? SupplierEstimatedDelivery { get; set; }

        /// <summary>Gets or sets the order version (Base64-encoded RowVersion for concurrency control)</summary>
        public required string Version { get; set; }

        /// <summary>Gets or sets when the order was created</summary>
        public required DateTime CreatedAt { get; set; }

        /// <summary>Gets or sets when the order was last updated</summary>
        public required DateTime UpdatedAt { get; set; }

        /// <summary>Gets or sets who created the order</summary>
        public required string CreatedBy { get; set; }

        /// <summary>Gets or sets who last updated the order</summary>
        public required string UpdatedBy { get; set; }

        /// <summary>Gets or sets 3D printing specific attributes</summary>
        public Order3DPrintingAttributesDto? PrintingAttributes { get; set; }

        /// <summary>Gets or sets CNC machining specific attributes</summary>
        public OrderCncMachiningAttributesDto? CncAttributes { get; set; }

        /// <summary>Gets or sets sheet metal specific attributes</summary>
        public OrderSheetMetalAttributesDto? SheetMetalAttributes { get; set; }

        /// <summary>Gets or sets 3D scanning specific attributes</summary>
        public Order3DScanningAttributesDto? ScanningAttributes { get; set; }

        /// <summary>Gets or sets 3D design specific attributes</summary>
        public Order3DDesignAttributesDto? DesignAttributes { get; set; }
    }

    /// <summary>
    /// 3D printing service-specific attributes
    /// </summary>
    public class Order3DPrintingAttributesDto
    {
        /// <summary>Gets or sets if thread/tap is required</summary>
        public bool ThreadTapRequired { get; set; }

        /// <summary>Gets or sets if inserts are required</summary>
        public bool InsertRequired { get; set; }

        /// <summary>Gets or sets the part marking text</summary>
        public string? PartMarking { get; set; }

        /// <summary>Gets or sets if assembly testing is required</summary>
        public bool PartAssemblyTestRequired { get; set; }
    }

    /// <summary>
    /// CNC machining service-specific attributes
    /// </summary>
    public class OrderCncMachiningAttributesDto
    {
        /// <summary>Gets or sets if tapping is required</summary>
        public bool TapRequired { get; set; }

        /// <summary>Gets or sets the tolerance specification</summary>
        public string? Tolerance { get; set; }

        /// <summary>Gets or sets the surface finish requirement</summary>
        public string? SurfaceFinish { get; set; }

        /// <summary>Gets or sets the surface roughness requirement</summary>
        public string? SurfaceRoughness { get; set; }

        /// <summary>Gets or sets the inspection type</summary>
        public string? InspectionType { get; set; }
    }

    /// <summary>
    /// Sheet metal service-specific attributes
    /// </summary>
    public class OrderSheetMetalAttributesDto
    {
        /// <summary>Gets or sets the sheet thickness</summary>
        public string? Thickness { get; set; }

        /// <summary>Gets or sets if welding is required</summary>
        public bool WeldingRequired { get; set; }

        /// <summary>Gets or sets the welding details</summary>
        public string? WeldingDetails { get; set; }

        /// <summary>Gets or sets the tolerance specification</summary>
        public string? Tolerance { get; set; }

        /// <summary>Gets or sets the inspection type</summary>
        public string? InspectionType { get; set; }
    }

    /// <summary>
    /// 3D scanning service-specific attributes
    /// </summary>
    public class Order3DScanningAttributesDto
    {
        /// <summary>Gets or sets the required accuracy</summary>
        public string? RequiredAccuracy { get; set; }

        /// <summary>Gets or sets the scan location</summary>
        public string? ScanLocation { get; set; }

        /// <summary>Gets or sets the scan output format</summary>
        public string? OutputFileFormats { get; set; }

        /// <summary>Gets or sets if deviation report is requested</summary>
        public bool DeviationReportRequested { get; set; }
    }

    /// <summary>
    /// 3D design service-specific attributes
    /// </summary>
    public class Order3DDesignAttributesDto
    {
        /// <summary>Gets or sets the design software used</summary>
        public string? DesignSoftware { get; set; }

        /// <summary>Gets or sets the deliverables</summary>
        public string? Deliverables { get; set; }

        /// <summary>Gets or sets the design complexity level</summary>
        public string? ComplexityLevel { get; set; }

        /// <summary>Gets or sets the revision rounds</summary>
        public int RevisionRounds { get; set; }
    }
}
