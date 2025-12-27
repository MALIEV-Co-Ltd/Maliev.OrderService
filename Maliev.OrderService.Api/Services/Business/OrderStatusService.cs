using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Api.Mapping;
using Maliev.OrderService.Data;
using Maliev.OrderService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Maliev.OrderService.Api.Services.Business
{
    /// <summary>
    /// Service for managing order statuses
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="OrderStatusService"/> class.
    /// </remarks>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger instance</param>
    public partial class OrderStatusService(OrderDbContext context, ILogger<OrderStatusService> logger) : IOrderStatusService
    {
        private readonly OrderDbContext _context = context;
        private readonly ILogger<OrderStatusService> _logger = logger;

        /// <inheritdoc />
        public async Task<List<OrderStatusResponse>> GetOrderStatusHistoryAsync(string orderId, CancellationToken cancellationToken = default)
        {
            List<OrderStatus> statuses = await _context.OrderStatuses
                .Where(s => s.OrderId == orderId)
                .OrderBy(s => s.Timestamp)
                .ToListAsync(cancellationToken);

            return [.. statuses.Select(s => s.ToOrderStatusResponse())];
        }

        /// <inheritdoc />
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
            OrderStatus? currentStatus = await _context.OrderStatuses
                .Where(s => s.OrderId == orderId)
                .OrderByDescending(s => s.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);

            // Validate state transition (basic validation - full validation would check ValidTransitions)
            if (currentStatus?.Status == request.Status)
            {
                throw new InvalidOperationException($"Order is already in {request.Status} status");
            }

            var newStatus = request.ToOrderStatus();
            newStatus.OrderId = orderId;
            newStatus.UpdatedBy = updatedBy;
            newStatus.Timestamp = DateTime.UtcNow;

            _ = _context.OrderStatuses.Add(newStatus);
            _ = await _context.SaveChangesAsync(cancellationToken);

            return newStatus.ToOrderStatusResponse();
        }

        private static partial class Log
        {
            // LoggerMessage delegates can be added here as needed
        }
    }
}
