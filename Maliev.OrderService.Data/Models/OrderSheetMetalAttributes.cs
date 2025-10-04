namespace Maliev.OrderService.Data.Models;

public class OrderSheetMetalAttributes
{
    public string OrderId { get; set; } = null!; // PK and FK
    public string? Thickness { get; set; }
    public bool WeldingRequired { get; set; }
    public string? WeldingDetails { get; set; }
    public string? Tolerance { get; set; }
    public string? InspectionType { get; set; }

    // Navigation Properties
    public Order Order { get; set; } = null!;
}
