namespace Maliev.OrderService.Api.DTOs.Request;

public class UpdateOrderRequest
{
    public required string Version { get; set; } // Base64-encoded RowVersion for optimistic concurrency
    public string? Requirements { get; set; }
    public int? OrderedQuantity { get; set; }
    public int? ManufacturedQuantity { get; set; }
    public int? MaterialId { get; set; }
    public int? ColorId { get; set; }
    public int? SurfaceFinishingId { get; set; }
    public int? LeadTimeDays { get; set; }
    public DateTime? PromisedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public decimal? QuotedAmount { get; set; }
    public string? QuoteCurrency { get; set; }
    public string? AssignedEmployeeId { get; set; }
    public string? DepartmentId { get; set; }
}
