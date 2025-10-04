namespace Maliev.OrderService.Data.Models;

public class ServiceCategory
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public ICollection<ProcessType> ProcessTypes { get; set; } = new List<ProcessType>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
