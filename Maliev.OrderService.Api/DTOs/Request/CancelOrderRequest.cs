namespace Maliev.OrderService.Api.DTOs.Request;

public class CancelOrderRequest
{
    public required string CancellationReason { get; set; }
    public string? CustomerNotes { get; set; }
}
