using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;

namespace Maliev.OrderService.Api.Services.Business;

public interface IOrderStatusService
{
    Task<List<OrderStatusResponse>> GetOrderStatusHistoryAsync(string orderId, CancellationToken cancellationToken = default);
    Task<OrderStatusResponse> CreateOrderStatusAsync(string orderId, CreateOrderStatusRequest request, string updatedBy, CancellationToken cancellationToken = default);
}
