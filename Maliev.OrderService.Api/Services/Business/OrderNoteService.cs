using AutoMapper;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Data;
using Maliev.OrderService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Maliev.OrderService.Api.Services.Business;

public partial class OrderNoteService : IOrderNoteService
{
    private readonly OrderDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderNoteService> _logger;

    public OrderNoteService(OrderDbContext context, IMapper mapper, ILogger<OrderNoteService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<OrderNoteResponse>> GetOrderNotesAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var notes = await _context.OrderNotes
            .Where(n => n.OrderId == orderId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<OrderNoteResponse>>(notes);
    }

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

        var note = _mapper.Map<OrderNote>(request);
        note.OrderId = orderId;
        note.CreatedBy = createdBy;
        note.CreatedAt = DateTime.UtcNow;

        _context.OrderNotes.Add(note);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<OrderNoteResponse>(note);
    }

    private static partial class Log
    {
        // LoggerMessage delegates can be added here as needed
    }
}
