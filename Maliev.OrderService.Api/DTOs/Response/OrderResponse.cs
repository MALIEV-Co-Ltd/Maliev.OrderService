namespace Maliev.OrderService.Api.DTOs.Response;

public class OrderResponse
{
    public required string OrderId { get; set; }
    public required string CustomerId { get; set; }
    public required string CustomerType { get; set; }
    public required int ServiceCategoryId { get; set; }
    public string? ServiceCategoryName { get; set; }
    public int? ProcessTypeId { get; set; }
    public string? ProcessTypeName { get; set; }
    public string? CurrentStatus { get; set; } // Derived from latest OrderStatus
    public string? Requirements { get; set; }
    public bool IsConfidential { get; set; }
    public int? OrderedQuantity { get; set; }
    public int? ManufacturedQuantity { get; set; }
    public int? MaterialId { get; set; }
    public int? ColorId { get; set; }
    public int? SurfaceFinishingId { get; set; }
    public string? MaterialName { get; set; }
    public string? ColorName { get; set; }
    public string? SurfaceFinishingName { get; set; }
    public DateTime? MaterialCacheUpdatedAt { get; set; }
    public int? LeadTimeDays { get; set; }
    public DateTime? PromisedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public decimal? QuotedAmount { get; set; }
    public string? QuoteCurrency { get; set; }
    public string? PaymentId { get; set; }
    public string PaymentStatus { get; set; } = "Unpaid";
    public string? AssignedEmployeeId { get; set; }
    public string? DepartmentId { get; set; }
    public required string Version { get; set; } // Base64-encoded RowVersion
    public required DateTime CreatedAt { get; set; }
    public required DateTime UpdatedAt { get; set; }
    public required string CreatedBy { get; set; }
    public required string UpdatedBy { get; set; }

    // Service-specific attributes
    public Order3DPrintingAttributesDto? PrintingAttributes { get; set; }
    public OrderCncMachiningAttributesDto? CncAttributes { get; set; }
    public OrderSheetMetalAttributesDto? SheetMetalAttributes { get; set; }
    public Order3DScanningAttributesDto? ScanningAttributes { get; set; }
    public Order3DDesignAttributesDto? DesignAttributes { get; set; }
}

public class Order3DPrintingAttributesDto
{
    public string? InfillPercentage { get; set; }
    public string? LayerHeight { get; set; }
    public string? SupportType { get; set; }
    public string? Color { get; set; }
    public string? Finish { get; set; }
}

public class OrderCncMachiningAttributesDto
{
    public string? Tolerance { get; set; }
    public string? SurfaceFinish { get; set; }
    public string? ThreadType { get; set; }
}

public class OrderSheetMetalAttributesDto
{
    public string? SheetThickness { get; set; }
    public string? BendingMethod { get; set; }
    public string? WeldingType { get; set; }
    public string? CoatingType { get; set; }
}

public class Order3DScanningAttributesDto
{
    public string? ScanResolution { get; set; }
    public string? ScanFormat { get; set; }
    public string? ObjectSize { get; set; }
}

public class Order3DDesignAttributesDto
{
    public string? DesignSoftware { get; set; }
    public string? FileFormat { get; set; }
    public string? DesignComplexity { get; set; }
}
