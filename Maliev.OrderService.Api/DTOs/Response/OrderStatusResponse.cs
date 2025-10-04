namespace Maliev.OrderService.Api.DTOs.Response;

public class OrderStatusResponse
{
    public required long StatusId { get; set; }
    public required string OrderId { get; set; }
    public required string Status { get; set; }
    public string? InternalNotes { get; set; } // Only visible to employees
    public string? CustomerNotes { get; set; }
    public required string UpdatedBy { get; set; }
    public required DateTime Timestamp { get; set; }
}
