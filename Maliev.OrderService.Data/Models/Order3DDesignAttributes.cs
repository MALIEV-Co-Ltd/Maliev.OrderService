namespace Maliev.OrderService.Data.Models;

public class Order3DDesignAttributes
{
    public string OrderId { get; set; } = null!; // PK and FK
    public string? ComplexityLevel { get; set; } // Simple, Medium, Complex
    public string? Deliverables { get; set; } // CSV list
    public string? DesignSoftware { get; set; }
    public int RevisionRounds { get; set; } = 2;

    // Navigation Properties
    public Order Order { get; set; } = null!;
}
