namespace Maliev.OrderService.Data.Models;

public class OrderCncMachiningAttributes
{
    public string OrderId { get; set; } = null!; // PK and FK
    public bool TapRequired { get; set; }
    public string? Tolerance { get; set; }
    public string? SurfaceRoughness { get; set; }
    public string? InspectionType { get; set; }

    // Navigation Properties
    public Order Order { get; set; } = null!;
}
