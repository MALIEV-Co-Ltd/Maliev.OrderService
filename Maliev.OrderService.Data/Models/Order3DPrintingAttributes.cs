namespace Maliev.OrderService.Data.Models;

public class Order3DPrintingAttributes
{
    public string OrderId { get; set; } = null!; // PK and FK
    public bool ThreadTapRequired { get; set; }
    public bool InsertRequired { get; set; }
    public string? PartMarking { get; set; }
    public bool PartAssemblyTestRequired { get; set; }

    // Navigation Properties
    public Order Order { get; set; } = null!;
}
