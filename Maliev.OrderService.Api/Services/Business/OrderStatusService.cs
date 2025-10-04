using AutoMapper;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Data;
using Maliev.OrderService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Maliev.OrderService.Api.Services.Business;

public partial class OrderStatusService : IOrderStatusService
{
    private readonly OrderDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderStatusService> _logger;

    public OrderStatusService(OrderDbContext context, IMapper mapper, ILogger<OrderStatusService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<OrderStatusResponse>> GetOrderStatusHistoryAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var statuses = await _context.OrderStatuses
            .Where(s => s.OrderId == orderId)
            .OrderBy(s => s.Timestamp)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<OrderStatusResponse>>(statuses);
    }

    public async Task<OrderStatusResponse> CreateOrderStatusAsync(
        string orderId,
        CreateOrderStatusRequest request,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        // Verify order exists
        var orderExists = await _context.Orders.AnyAsync(o => o.OrderId == orderId, cancellationToken);
        if (!orderExists)
        {
            throw new InvalidOperationException($"Order {orderId} not found");
        }

        // Get current status
        var currentStatus = await _context.OrderStatuses
            .Where(s => s.OrderId == orderId)
            .OrderByDescending(s => s.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        // Validate state transition (basic validation - full validation would check ValidTransitions)
        if (currentStatus?.Status == request.Status)
        {
            throw new InvalidOperationException($"Order is already in {request.Status} status");
        }

        var newStatus = _mapper.Map<OrderStatus>(request);
        newStatus.OrderId = orderId;
        newStatus.UpdatedBy = updatedBy;
        newStatus.Timestamp = DateTime.UtcNow;

        _context.OrderStatuses.Add(newStatus);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<OrderStatusResponse>(newStatus);
    }

    private static partial class Log
    {
        // LoggerMessage delegates can be added here as needed
    }
}
