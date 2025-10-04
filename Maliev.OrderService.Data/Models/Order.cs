namespace Maliev.OrderService.Data.Models;

public class Order
{
    // Primary Key
    public string OrderId { get; set; } = null!;

    // Customer Information
    public string CustomerId { get; set; } = null!;
    public string CustomerType { get; set; } = null!; // "Customer" or "Employee"

    // Service Classification
    public int ServiceCategoryId { get; set; }
    public int? ProcessTypeId { get; set; } // Nullable - consulting/procurement may not have process

    // Material Information (External Service Integration)
    public int? MaterialId { get; set; }
    public int? ColorId { get; set; }
    public int? SurfaceFinishingId { get; set; }
    public string? MaterialName { get; set; } // Cached display name (24-hour TTL)
    public string? ColorName { get; set; } // Cached display name (24-hour TTL)
    public string? SurfaceFinishingName { get; set; } // Cached display name (24-hour TTL)
    public DateTime? MaterialCacheUpdatedAt { get; set; }

    // Quantity Tracking
    public int? OrderedQuantity { get; set; } // Nullable - manufacturing orders only
    public int? ManufacturedQuantity { get; set; } // Running total of completed units
    // RemainingQuantity is computed: ordered_quantity - manufactured_quantity

    // Date Tracking
    public int? LeadTimeDays { get; set; }
    public DateTime? PromisedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }

    // Quoting Information
    public decimal? QuotedAmount { get; set; }
    public string? QuoteCurrency { get; set; } = "THB";

    // Confidentiality (Auto-set based on NDA)
    public bool IsConfidential { get; set; }

    // Payment Information (External Service Integration)
    public string? PaymentId { get; set; }
    public string PaymentStatus { get; set; } = "Unpaid"; // Unpaid, Paid, POIssued

    // Assignment Information
    public string? AssignedEmployeeId { get; set; }
    public string? DepartmentId { get; set; }

    // Requirements
    public string? Requirements { get; set; }

    // Audit Fields
    public byte[] Version { get; set; } = null!; // RowVersion for optimistic concurrency
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = null!;
    public string UpdatedBy { get; set; } = null!;

    // Navigation Properties
    public ServiceCategory ServiceCategory { get; set; } = null!;
    public ProcessType? ProcessType { get; set; }
    public ICollection<OrderStatus> OrderStatuses { get; set; } = new List<OrderStatus>();
    public ICollection<OrderFile> OrderFiles { get; set; } = new List<OrderFile>();
    public ICollection<OrderNote> OrderNotes { get; set; } = new List<OrderNote>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    // Service-Specific Attributes (One-to-One)
    public Order3DPrintingAttributes? PrintingAttributes { get; set; }
    public OrderCncMachiningAttributes? CncAttributes { get; set; }
    public OrderSheetMetalAttributes? SheetMetalAttributes { get; set; }
    public Order3DScanningAttributes? ScanningAttributes { get; set; }
    public Order3DDesignAttributes? DesignAttributes { get; set; }
}
