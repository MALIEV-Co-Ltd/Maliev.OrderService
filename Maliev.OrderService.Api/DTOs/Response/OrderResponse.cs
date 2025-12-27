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

        /// <summary>Gets or sets the payment transaction ID</summary>
        public string? PaymentId { get; set; }

        /// <summary>Gets or sets the payment status</summary>
        public string PaymentStatus { get; set; } = "Unpaid";

        /// <summary>Gets or sets the assigned employee ID</summary>
        public string? AssignedEmployeeId { get; set; }

        /// <summary>Gets or sets the department ID</summary>
        public string? DepartmentId { get; set; }

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
        /// <summary>Gets or sets the infill percentage</summary>
        public string? InfillPercentage { get; set; }

        /// <summary>Gets or sets the layer height</summary>
        public string? LayerHeight { get; set; }

        /// <summary>Gets or sets the support type</summary>
        public string? SupportType { get; set; }

        /// <summary>Gets or sets the color</summary>
        public string? Color { get; set; }

        /// <summary>Gets or sets the finish</summary>
        public string? Finish { get; set; }
    }

    /// <summary>
    /// CNC machining service-specific attributes
    /// </summary>
    public class OrderCncMachiningAttributesDto
    {
        /// <summary>Gets or sets the tolerance specification</summary>
        public string? Tolerance { get; set; }

        /// <summary>Gets or sets the surface finish requirement</summary>
        public string? SurfaceFinish { get; set; }

        /// <summary>Gets or sets the thread type</summary>
        public string? ThreadType { get; set; }
    }

    /// <summary>
    /// Sheet metal service-specific attributes
    /// </summary>
    public class OrderSheetMetalAttributesDto
    {
        /// <summary>Gets or sets the sheet thickness</summary>
        public string? SheetThickness { get; set; }

        /// <summary>Gets or sets the bending method</summary>
        public string? BendingMethod { get; set; }

        /// <summary>Gets or sets the welding type</summary>
        public string? WeldingType { get; set; }

        /// <summary>Gets or sets the coating type</summary>
        public string? CoatingType { get; set; }
    }

    /// <summary>
    /// 3D scanning service-specific attributes
    /// </summary>
    public class Order3DScanningAttributesDto
    {
        /// <summary>Gets or sets the scan resolution</summary>
        public string? ScanResolution { get; set; }

        /// <summary>Gets or sets the scan output format</summary>
        public string? ScanFormat { get; set; }

        /// <summary>Gets or sets the object size</summary>
        public string? ObjectSize { get; set; }
    }

    /// <summary>
    /// 3D design service-specific attributes
    /// </summary>
    public class Order3DDesignAttributesDto
    {
        /// <summary>Gets or sets the design software used</summary>
        public string? DesignSoftware { get; set; }

        /// <summary>Gets or sets the file format</summary>
        public string? FileFormat { get; set; }

        /// <summary>Gets or sets the design complexity level</summary>
        public string? DesignComplexity { get; set; }
    }
}
