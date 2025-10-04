using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;

namespace Maliev.OrderService.Api.Services.Business;

public interface IOrderManagementService
{
    Task<OrderResponse?> GetOrderByIdAsync(string orderId, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<OrderResponse>> GetOrdersAsync(int page, int pageSize, string? customerId = null, string? status = null, CancellationToken cancellationToken = default);
    Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, string createdBy, CancellationToken cancellationToken = default);
    Task<OrderResponse> UpdateOrderAsync(string orderId, UpdateOrderRequest request, string updatedBy, CancellationToken cancellationToken = default);
    Task<bool> CancelOrderAsync(string orderId, string cancelledBy, string? reason = null, CancellationToken cancellationToken = default);
}
