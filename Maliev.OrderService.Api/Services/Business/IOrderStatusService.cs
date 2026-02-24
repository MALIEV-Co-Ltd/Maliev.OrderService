using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;

namespace Maliev.OrderService.Api.Services.Business
{
    /// <summary>
    /// Service for managing order status history
    /// </summary>
    public interface IOrderStatusService
    {
        /// <summary>
        /// Gets the status history for an order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of status history entries</returns>
        Task<List<OrderStatusResponse>> GetOrderStatusHistoryAsync(string orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new status entry for an order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="request">Status creation request</param>
        /// <param name="updatedBy">User who updated the status</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created status entry</returns>
        Task<OrderStatusResponse> CreateOrderStatusAsync(string orderId, CreateOrderStatusRequest request, string updatedBy, CancellationToken cancellationToken = default);
    }
}
