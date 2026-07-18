namespace Maliev.OrderService.Application.DTOs.Request
{
    /// <summary>
    /// Request model for cancelling an order with a reason.
    /// </summary>
    public class CancelOrderRequest
    {
        /// <summary>Gets or sets the cancellation reason.</summary>
        public string? CancellationReason { get; set; }
    }
}
