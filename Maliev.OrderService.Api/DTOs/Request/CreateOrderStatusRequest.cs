namespace Maliev.OrderService.Api.DTOs.Request;

public class CreateOrderStatusRequest
{
    public required string Status { get; set; }
    public string? InternalNotes { get; set; }
    public string? CustomerNotes { get; set; }
}
