using System.Globalization;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Api.Mapping;
using Maliev.OrderService.Data;
using Maliev.OrderService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Maliev.OrderService.Api.Services.Business
{
    /// <summary>
    /// Service for managing orders
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="OrderManagementService"/> class.
    /// </remarks>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger instance</param>
    public partial class OrderManagementService(OrderDbContext context, ILogger<OrderManagementService> logger) : IOrderManagementService
    {
        private readonly OrderDbContext _context = context;
        private readonly ILogger<OrderManagementService> _logger = logger;

        /// <inheritdoc />
        public async Task<OrderResponse?> GetOrderByIdAsync(string orderId, CancellationToken cancellationToken = default)
        {
            Order? order = await _context.Orders
                .Include(o => o.ServiceCategory)
                .Include(o => o.ProcessType)
                .Include(o => o.OrderStatuses)
                .Include(o => o.PrintingAttributes)
                .Include(o => o.CncAttributes)
                .Include(o => o.SheetMetalAttributes)
                .Include(o => o.ScanningAttributes)
                .Include(o => o.DesignAttributes)
                .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

            return order?.ToOrderResponse();
        }

        /// <inheritdoc />
        public async Task<PaginatedResponse<OrderResponse>> GetOrdersAsync(
            int page,
            int pageSize,
            string? customerId = null,
            string? status = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Order> query = _context.Orders
                .Include(o => o.ServiceCategory)
                .Include(o => o.ProcessType)
                .Include(o => o.OrderStatuses)
                .AsQueryable();

            if (!string.IsNullOrEmpty(customerId))
            {
                query = query.Where(o => o.CustomerId == customerId);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            List<Order> items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginatedResponse<OrderResponse>
            {
                Items = [.. items.Select(o => o.ToOrderResponse())],
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        /// <inheritdoc />
        public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, string createdBy, CancellationToken cancellationToken = default)
        {
            const int maxRetries = 3;
            int retryCount = 0;

            while (true)
            {
                try
                {
                    var order = request.ToOrder();
                    order.OrderId = await GenerateOrderIdAsync(cancellationToken);
                    order.CreatedBy = createdBy;
                    order.UpdatedBy = createdBy;
                    order.CreatedAt = DateTime.UtcNow;
                    order.UpdatedAt = DateTime.UtcNow;

                    _ = _context.Orders.Add(order);

                    var initialStatus = new OrderStatus
                    {
                        OrderId = order.OrderId,
                        Status = "New",
                        UpdatedBy = createdBy,
                        Timestamp = DateTime.UtcNow
                    };
                    _ = _context.OrderStatuses.Add(initialStatus);

                    _ = await _context.SaveChangesAsync(cancellationToken);

                    return order.ToOrderResponse();
                }
                catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" } && retryCount < maxRetries)
                {
                    // Unique constraint violation - likely a race condition in GenerateOrderIdAsync
                    retryCount++;
                    Log.RaceConditionDetected(_logger, retryCount, maxRetries);

                    // Clear the tracker to avoid issues with the failed entity
                    _context.ChangeTracker.Clear();

                    // Small random delay to desynchronize
                    await Task.Delay(Random.Shared.Next(10, 50), cancellationToken);
                }
            }
        }

        /// <inheritdoc />
        public async Task<OrderResponse> UpdateOrderAsync(string orderId, UpdateOrderRequest request, string updatedBy, CancellationToken cancellationToken = default)
        {
            Order? order = await _context.Orders.FindAsync([orderId], cancellationToken) ?? throw new InvalidOperationException($"Order {orderId} not found");

            // Optimistic concurrency check
            byte[] requestVersion;
            try
            {
                requestVersion = Convert.FromBase64String(request.Version);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException($"Invalid version format for order {orderId}. Version must be a valid Base64 string.");
            }

            if (!order.Version.SequenceEqual(requestVersion))
            {
                throw new DbUpdateConcurrencyException("Order has been modified by another user");
            }

            order.UpdateOrder(request);
            order.UpdatedBy = updatedBy;
            order.UpdatedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            return order.ToOrderResponse();
        }

        /// <inheritdoc />
        public async Task<bool> CancelOrderAsync(string orderId, string cancelledBy, string? reason = null, CancellationToken cancellationToken = default)
        {
            Order? order = await _context.Orders.FindAsync([orderId], cancellationToken);
            if (order == null)
            {
                return false;
            }

            var cancelStatus = new OrderStatus
            {
                OrderId = orderId,
                Status = "Cancelled",
                InternalNotes = reason,
                UpdatedBy = cancelledBy,
                Timestamp = DateTime.UtcNow
            };

            _ = _context.OrderStatuses.Add(cancelStatus);
            _ = await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        private async Task<string> GenerateOrderIdAsync(CancellationToken cancellationToken)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"ORD-{year}-";

            Order? lastOrder = await _context.Orders
                .Where(o => o.OrderId.StartsWith(prefix))
                .OrderByDescending(o => o.OrderId)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastOrder == null)
            {
                return $"{prefix}00001";
            }

            var lastNumber = int.Parse(lastOrder.OrderId.AsSpan(prefix.Length), CultureInfo.InvariantCulture);
            return $"{prefix}{lastNumber + 1:D5}";
        }

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Warning, Message = "Race condition detected during OrderId generation. Retrying {RetryCount}/{MaxRetries}...")]
            public static partial void RaceConditionDetected(ILogger logger, int retryCount, int maxRetries);
        }
    }
}
