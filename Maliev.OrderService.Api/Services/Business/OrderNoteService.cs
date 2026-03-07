using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Api.Mapping;
using Maliev.OrderService.Infrastructure.Persistence;
using Maliev.OrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maliev.OrderService.Api.Services.Business
{
    /// <summary>
    /// Service for managing order notes
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="OrderNoteService"/> class.
    /// </remarks>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger instance</param>
    public partial class OrderNoteService(OrderDbContext context, ILogger<OrderNoteService> logger) : IOrderNoteService
    {
        private readonly OrderDbContext _context = context;
        private readonly ILogger<OrderNoteService> _logger = logger;

        /// <inheritdoc />
        public async Task<List<OrderNoteResponse>> GetOrderNotesAsync(string orderId, CancellationToken cancellationToken = default)
        {
            List<OrderNote> notes = await _context.OrderNotes
                .Where(n => n.OrderId == orderId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);

            return [.. notes.Select(n => n.ToOrderNoteResponse())];
        }

        /// <inheritdoc />
        public async Task<OrderNoteResponse> CreateOrderNoteAsync(
            string orderId,
            CreateOrderNoteRequest request,
            string createdBy,
            CancellationToken cancellationToken = default)
        {
            // Verify order exists
            bool orderExists = await _context.Orders.AnyAsync(o => o.OrderId == orderId, cancellationToken);
            if (!orderExists)
            {
                throw new InvalidOperationException($"Order {orderId} not found");
            }

            var note = request.ToOrderNote();
            note.OrderId = orderId;
            note.CreatedBy = createdBy;
            note.CreatedAt = DateTime.UtcNow;

            _ = _context.OrderNotes.Add(note);
            _ = await _context.SaveChangesAsync(cancellationToken);

            return note.ToOrderNoteResponse();
        }

        private static partial class Log
        {
            // LoggerMessage delegates can be added here as needed
        }
    }
}
