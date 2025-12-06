namespace Maliev.OrderService.Api.DTOs.Request;

/// <summary>
/// Request model for canceling an order with a reason
/// </summary>
public class CancelOrderRequest
{
    /// <summary>Gets or sets the reason for cancellation</summary>
    public required string CancellationReason { get; set; }
    
    /// <summary>Gets or sets customer-facing notes about the cancellation</summary>
    public string? CustomerNotes { get; set; }
}
