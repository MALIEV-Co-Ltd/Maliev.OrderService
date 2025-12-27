namespace Maliev.OrderService.Api.DTOs.Request
{
    /// <summary>
    /// Request model for batch creating multiple orders
    /// </summary>
    public class BatchCreateOrderRequest
    {
        /// <summary>Gets or sets the list of orders to create</summary>
        public required List<CreateOrderRequest> Orders { get; set; }
    }
}
