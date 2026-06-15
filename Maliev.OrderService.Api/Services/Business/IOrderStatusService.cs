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

        /// <summary>
        /// Marks the order payment state as processing without changing the order lifecycle status.
        /// </summary>
        /// <param name="orderId">The order ID or order number.</param>
        /// <param name="paymentId">The payment transaction identifier.</param>
        /// <param name="providerName">The payment provider name.</param>
        /// <param name="providerEventCode">The provider event code that triggered the update.</param>
        /// <param name="updatedBy">The system or user performing the update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task MarkPaymentProcessingAsync(
            string orderId,
            string paymentId,
            string providerName,
            string providerEventCode,
            string updatedBy,
            CancellationToken cancellationToken = default);
    }
}
