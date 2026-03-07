namespace Maliev.OrderService.Domain.Entities
{
    /// <summary>
    /// Represents a customer manufacturing order.
    /// </summary>
    public class Order
    {
        // Primary Key
        /// <summary>Gets or sets the unique order identifier.</summary>
        public string OrderId { get; set; } = null!;

        // Customer Information
        /// <summary>Gets or sets the customer identifier.</summary>
        public string CustomerId { get; set; } = null!;

        /// <summary>Gets or sets the customer type ("Customer" or "Employee").</summary>
        public string CustomerType { get; set; } = null!;

        // Service Classification
        /// <summary>Gets or sets the service category identifier.</summary>
        public int ServiceCategoryId { get; set; }

        /// <summary>Gets or sets the optional process type identifier.</summary>
        public int? ProcessTypeId { get; set; }

        // Material Information (External Service Integration)
        /// <summary>Gets or sets the material identifier.</summary>
        public int? MaterialId { get; set; }

        /// <summary>Gets or sets the color identifier.</summary>
        public int? ColorId { get; set; }

        /// <summary>Gets or sets the surface finishing identifier.</summary>
        public int? SurfaceFinishingId { get; set; }

        /// <summary>Gets or sets the cached material display name (24-hour TTL).</summary>
        public string? MaterialName { get; set; }

        /// <summary>Gets or sets the cached color display name (24-hour TTL).</summary>
        public string? ColorName { get; set; }

        /// <summary>Gets or sets the cached surface finishing display name (24-hour TTL).</summary>
        public string? SurfaceFinishingName { get; set; }

        /// <summary>Gets or sets when the material cache was last updated.</summary>
        public DateTime? MaterialCacheUpdatedAt { get; set; }

        // Quantity Tracking
        /// <summary>Gets or sets the ordered quantity (nullable for non-manufacturing orders).</summary>
        public int? OrderedQuantity { get; set; }

        /// <summary>Gets or sets the running total of manufactured units.</summary>
        public int? ManufacturedQuantity { get; set; }

        // Date Tracking
        /// <summary>Gets or sets the lead time in days.</summary>
        public int? LeadTimeDays { get; set; }

        /// <summary>Gets or sets the promised delivery date.</summary>
        public DateTime? PromisedDeliveryDate { get; set; }

        /// <summary>Gets or sets the actual delivery date.</summary>
        public DateTime? ActualDeliveryDate { get; set; }

        // Quoting Information
        /// <summary>Gets or sets the quoted amount.</summary>
        public decimal? QuotedAmount { get; set; }

        /// <summary>Gets or sets the quote currency code.</summary>
        public string? QuoteCurrency { get; set; } = "THB";

        // Confidentiality (Auto-set based on NDA)
        /// <summary>Gets or sets a value indicating whether this order is confidential.</summary>
        public bool IsConfidential { get; set; }

        // Payment Information (External Service Integration)
        /// <summary>Gets or sets the payment transaction identifier.</summary>
        public string? PaymentId { get; set; }

        /// <summary>Gets or sets the payment status (Unpaid, Paid, POIssued).</summary>
        public string PaymentStatus { get; set; } = "Unpaid";

        // Customer Purchase Order (Inbound)
        /// <summary>Gets or sets the customer purchase order number.</summary>
        public string? CustomerPoNumber { get; set; }

        /// <summary>Gets or sets the customer purchase order file identifier.</summary>
        public Guid? CustomerPoFileId { get; set; }

        // Assignment Information
        /// <summary>Gets or sets the assigned employee identifier.</summary>
        public string? AssignedEmployeeId { get; set; }

        /// <summary>Gets or sets the department identifier.</summary>
        public string? DepartmentId { get; set; }

        // Requirements
        /// <summary>Gets or sets the order requirements and specifications.</summary>
        public string? Requirements { get; set; }

        // Audit Fields
        /// <summary>Gets or sets when the order was created.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Gets or sets when the order was last updated.</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Gets or sets who created the order.</summary>
        public string CreatedBy { get; set; } = null!;

        /// <summary>Gets or sets who last updated the order.</summary>
        public string UpdatedBy { get; set; } = null!;

        // Navigation Properties
        /// <summary>Gets or sets the service category navigation property.</summary>
        public ServiceCategory ServiceCategory { get; set; } = null!;

        /// <summary>Gets or sets the process type navigation property.</summary>
        public ProcessType? ProcessType { get; set; }

        /// <summary>Gets or sets the collection of order statuses.</summary>
        public ICollection<OrderStatus> OrderStatuses { get; set; } = [];

        /// <summary>Gets or sets the collection of order files.</summary>
        public ICollection<OrderFile> OrderFiles { get; set; } = [];

        /// <summary>Gets or sets the collection of order notes.</summary>
        public ICollection<OrderNote> OrderNotes { get; set; } = [];

        /// <summary>Gets or sets the collection of audit logs.</summary>
        public ICollection<AuditLog> AuditLogs { get; set; } = [];

        // Service-Specific Attributes (One-to-One)
        /// <summary>Gets or sets the 3D printing specific attributes.</summary>
        public Order3DPrintingAttributes? PrintingAttributes { get; set; }

        /// <summary>Gets or sets the CNC machining specific attributes.</summary>
        public OrderCncMachiningAttributes? CncAttributes { get; set; }

        /// <summary>Gets or sets the sheet metal specific attributes.</summary>
        public OrderSheetMetalAttributes? SheetMetalAttributes { get; set; }

        /// <summary>Gets or sets the 3D scanning specific attributes.</summary>
        public Order3DScanningAttributes? ScanningAttributes { get; set; }

        /// <summary>Gets or sets the 3D design specific attributes.</summary>
        public Order3DDesignAttributes? DesignAttributes { get; set; }
    }
}
