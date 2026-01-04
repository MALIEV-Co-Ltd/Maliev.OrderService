using System.Globalization;
using System.Security.Cryptography;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Api.Mapping;
using Maliev.OrderService.Data;
using Maliev.OrderService.Data.Models;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using Maliev.MessagingContracts.Generated;

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
    /// <param name="publishEndpoint">The MassTransit publish endpoint</param>
    public partial class OrderManagementService(
        OrderDbContext context,
        ILogger<OrderManagementService> logger,
        IPublishEndpoint publishEndpoint) : IOrderManagementService
    {
        private readonly OrderDbContext _context = context;
        private readonly ILogger<OrderManagementService> _logger = logger;
        private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;

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

            int totalCount = await query.CountAsync(cancellationToken);
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
        public async Task<OrderResponse> PrepareOrderForCreationAsync(CreateOrderRequest request, string createdBy, CancellationToken cancellationToken = default)
        {
            var order = request.ToOrder();
            order.OrderId = await GenerateOrderIdAsync(cancellationToken);
            order.CreatedBy = createdBy;
            order.UpdatedBy = createdBy;
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            order.Version = new byte[8]; // Initialize RowVersion for PostgreSQL

            _ = _context.Orders.Add(order);

            var initialStatus = new OrderStatus
            {
                OrderId = order.OrderId,
                Status = "New",
                UpdatedBy = createdBy,
                Timestamp = DateTime.UtcNow
            };
            _ = _context.OrderStatuses.Add(initialStatus);

            // Don't save changes - caller will save in batch
            return order.ToOrderResponse();
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
                    OrderResponse order = await PrepareOrderForCreationAsync(request, createdBy, cancellationToken);
                    _ = await _context.SaveChangesAsync(cancellationToken);

                    // Publish OrderCreatedEvent
                    await _publishEndpoint.Publish(new OrderCreatedEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: "OrderCreatedEvent",
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: OrderCreatedConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: DateTimeOffset.UtcNow,
                        IsPublic: false,
                        Payload: new OrderCreatedEventPayload(
                            OrderId: StringToGuid(order.OrderId),
                            OrderNumber: order.OrderId,
                            CustomerId: StringToGuid(order.CustomerId),
                            TotalAmount: (double)(order.QuotedAmount ?? 0),
                            Currency: order.QuoteCurrency ?? "THB",
                            CreatedAt: new DateTimeOffset(order.CreatedAt, TimeSpan.Zero),
                            AssignedEmployeeId: order.AssignedEmployeeId != null ? StringToGuid(order.AssignedEmployeeId) : null
                        )
                    ), cancellationToken);

                    Log.OrderCreatedEventPublished(_logger, order.OrderId);

                    return order;
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
            int year = DateTime.UtcNow.Year;
            string prefix = $"ORD-{year}-";

            // Check both database AND ChangeTracker for the highest OrderId
            Order? lastOrder = await _context.Orders
                .Where(o => o.OrderId.StartsWith(prefix))
                .OrderByDescending(o => o.OrderId)
                .FirstOrDefaultAsync(cancellationToken);

            // Also check pending orders in ChangeTracker (for batch operations)
            Order? pendingOrders = _context.ChangeTracker.Entries<Order>()
                .Where(e => e.State == EntityState.Added && e.Entity.OrderId.StartsWith(prefix, StringComparison.Ordinal))
                .Select(e => e.Entity)
                .OrderByDescending(o => o.OrderId)
                .FirstOrDefault();

            // Use whichever is higher
            string? highestOrderId = lastOrder?.OrderId;
            if (pendingOrders != null && (highestOrderId == null || string.CompareOrdinal(pendingOrders.OrderId, highestOrderId) > 0))
            {
                highestOrderId = pendingOrders.OrderId;
            }

            if (highestOrderId == null)
            {
                return $"{prefix}00001";
            }

            int lastNumber = int.Parse(highestOrderId.AsSpan(prefix.Length), CultureInfo.InvariantCulture);
            return $"{prefix}{lastNumber + 1:D5}";
        }

        /// <summary>
        /// Converts a string ID to a deterministic Guid using MD5 hashing
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "MD5 is used for deterministic hashing, not cryptography")]
        private static Guid StringToGuid(string value)
        {
            byte[] hash = MD5.HashData(System.Text.Encoding.UTF8.GetBytes(value));
            return new Guid(hash);
        }

        // Static readonly array for ConsumedBy list to avoid CA1861
        private static readonly string[] OrderCreatedConsumers = ["InvoiceService", "NotificationService"];

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Warning, Message = "Race condition detected during OrderId generation. Retrying {RetryCount}/{MaxRetries}...")]
            public static partial void RaceConditionDetected(ILogger logger, int retryCount, int maxRetries);

            [LoggerMessage(Level = LogLevel.Information, Message = "Published OrderCreatedEvent for order {OrderId}")]
            public static partial void OrderCreatedEventPublished(ILogger logger, string orderId);
        }
    }
}
