namespace Maliev.OrderService.Api.DTOs.Request;

public class BatchCreateOrderRequest
{
    public required List<CreateOrderRequest> Orders { get; set; }
}
