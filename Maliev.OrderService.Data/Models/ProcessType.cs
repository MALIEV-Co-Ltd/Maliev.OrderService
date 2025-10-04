namespace Maliev.OrderService.Data.Models;

public class ProcessType
{
    public int ProcessTypeId { get; set; }
    public int ServiceCategoryId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public ServiceCategory ServiceCategory { get; set; } = null!;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
