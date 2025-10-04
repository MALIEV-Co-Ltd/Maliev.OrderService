using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;

namespace Maliev.OrderService.Api.Services.Business;

public interface IOrderNoteService
{
    Task<List<OrderNoteResponse>> GetOrderNotesAsync(string orderId, CancellationToken cancellationToken = default);
    Task<OrderNoteResponse> CreateOrderNoteAsync(string orderId, CreateOrderNoteRequest request, string createdBy, CancellationToken cancellationToken = default);
}
