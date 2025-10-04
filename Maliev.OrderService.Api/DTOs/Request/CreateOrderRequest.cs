namespace Maliev.OrderService.Api.DTOs.Request;

public class CreateOrderRequest
{
    public required string CustomerId { get; set; }
    public required string CustomerType { get; set; } // "Customer" or "Employee"
    public required int ServiceCategoryId { get; set; }
    public int? ProcessTypeId { get; set; }
    public string? Requirements { get; set; }
    public bool IsConfidential { get; set; }
    public int? OrderedQuantity { get; set; }
    public int? MaterialId { get; set; }
    public int? ColorId { get; set; }
    public int? SurfaceFinishingId { get; set; }
    public int? LeadTimeDays { get; set; }
    public DateTime? PromisedDeliveryDate { get; set; }
    public string? AssignedEmployeeId { get; set; }
    public string? DepartmentId { get; set; }
}
