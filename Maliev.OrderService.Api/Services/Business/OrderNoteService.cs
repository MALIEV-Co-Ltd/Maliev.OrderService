using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Api.Mapping;
using Maliev.OrderService.Data;
using Maliev.OrderService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Maliev.OrderService.Api.Services.Business;

/// <summary>
/// Service for managing order notes
/// </summary>
public partial class OrderNoteService : IOrderNoteService
{
    private readonly OrderDbContext _context;
    private readonly ILogger<OrderNoteService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderNoteService"/> class.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger instance</param>
    public OrderNoteService(OrderDbContext context, ILogger<OrderNoteService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<OrderNoteResponse>> GetOrderNotesAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var notes = await _context.OrderNotes
            .Where(n => n.OrderId == orderId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return notes.Select(n => n.ToOrderNoteResponse()).ToList();
    }

    /// <inheritdoc />
    public async Task<OrderNoteResponse> CreateOrderNoteAsync(
        string orderId,
        CreateOrderNoteRequest request,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        // Verify order exists
        var orderExists = await _context.Orders.AnyAsync(o => o.OrderId == orderId, cancellationToken);
        if (!orderExists)
        {
            throw new InvalidOperationException($"Order {orderId} not found");
        }

        var note = request.ToOrderNote();
        note.OrderId = orderId;
        note.CreatedBy = createdBy;
        note.CreatedAt = DateTime.UtcNow;

        _context.OrderNotes.Add(note);
        await _context.SaveChangesAsync(cancellationToken);

        return note.ToOrderNoteResponse();
    }

    private static partial class Log
    {
        // LoggerMessage delegates can be added here as needed
    }
}
