using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;

namespace Maliev.OrderService.Api.Services.Business
{
    /// <summary>
    /// Service for managing order notes
    /// </summary>
    public interface IOrderNoteService
    {
        /// <summary>
        /// Gets all notes associated with an order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of order notes</returns>
        Task<List<OrderNoteResponse>> GetOrderNotesAsync(string orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new note for an order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="request">Note creation request</param>
        /// <param name="createdBy">User who created the note</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created note</returns>
        Task<OrderNoteResponse> CreateOrderNoteAsync(string orderId, CreateOrderNoteRequest request, string createdBy, CancellationToken cancellationToken = default);
    }
}
