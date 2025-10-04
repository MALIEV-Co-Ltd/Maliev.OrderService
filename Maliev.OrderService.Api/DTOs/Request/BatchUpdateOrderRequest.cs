namespace Maliev.OrderService.Api.DTOs.Request;

public class BatchUpdateOrderRequest
{
    public required string OrderId { get; set; }
    public required string Version { get; set; }
    public string? Requirements { get; set; }
    public int? OrderedQuantity { get; set; }
    public string? AssignedEmployeeId { get; set; }
    public string? DepartmentId { get; set; }
}
