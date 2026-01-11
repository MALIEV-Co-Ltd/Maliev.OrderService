using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using System.Security.Claims;

namespace Maliev.OrderService.Api.Services.Business
{
    /// <summary>
    /// Service for managing orders
    /// </summary>
    public interface IOrderManagementService
    {
        /// <summary>
        /// Gets an order by its ID
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The order response or null if not found</returns>
        Task<OrderResponse?> GetOrderByIdAsync(string orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a paginated list of orders
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="user">The user requesting the list (for authorization)</param>
        /// <param name="customerId">Optional customer ID filter</param>
        /// <param name="status">Optional status filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of orders</returns>
        Task<PaginatedResponse<OrderResponse>> GetOrdersAsync(int page, int pageSize, System.Security.Claims.ClaimsPrincipal user, string? customerId = null, string? status = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new order
        /// </summary>
        /// <param name="request">Order creation request</param>
        /// <param name="createdBy">User who created the order</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created order</returns>
        Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, string createdBy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prepares a new order for creation without saving (for batch operations)
        /// </summary>
        /// <param name="request">Order creation request</param>
        /// <param name="createdBy">User who created the order</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The prepared order entity (not yet saved)</returns>
        Task<Maliev.OrderService.Data.Models.Order> PrepareOrderEntityForCreationAsync(CreateOrderRequest request, string createdBy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="request">Order update request</param>
        /// <param name="updatedBy">User who updated the order</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated order</returns>
        Task<OrderResponse> UpdateOrderAsync(string orderId, UpdateOrderRequest request, string updatedBy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels an order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="cancelledBy">User who cancelled the order</param>
        /// <param name="reason">Optional cancellation reason</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false if not found</returns>
        Task<bool> CancelOrderAsync(string orderId, string cancelledBy, string? reason = null, CancellationToken cancellationToken = default);
    }
}
