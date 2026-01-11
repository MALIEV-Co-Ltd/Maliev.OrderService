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

            return order?.ToOrderResponse(_context.Entry(order).Property<uint>("xmin").CurrentValue);
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
            {}

            int totalCount = await query.CountAsync(cancellationToken);
            List<Order> items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginatedResponse<OrderResponse>
            {
                Items = [.. items.Select(o => o.ToOrderResponse(_context.Entry(o).Property<uint>("xmin").CurrentValue))],
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        /// <inheritdoc />
        public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, string createdBy, CancellationToken cancellationToken = default)
        {
            Order order = await PrepareOrderEntityForCreationAsync(request, createdBy, cancellationToken);
            _ = await _context.SaveChangesAsync(cancellationToken);

            // Now map to response with the REAL xmin from the DB
            uint? xmin = _context.Entry(order).Property<uint>("xmin").CurrentValue;
            var response = order.ToOrderResponse(xmin);

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
                    OrderId: StringToGuid(response.OrderId),
                    OrderNumber: response.OrderId,
                    CustomerId: StringToGuid(response.CustomerId),
                    TotalAmount: (double)(response.QuotedAmount ?? 0),
                    Currency: response.QuoteCurrency ?? "THB",
                    CreatedAt: new DateTimeOffset(response.CreatedAt, TimeSpan.Zero),
                    AssignedEmployeeId: response.AssignedEmployeeId != null ? StringToGuid(response.AssignedEmployeeId) : null
                )
            ), cancellationToken);

            Log.OrderCreatedEventPublished(_logger, response.OrderId);

            return response;
        }

        /// <inheritdoc />
        public async Task<Order> PrepareOrderEntityForCreationAsync(CreateOrderRequest request, string createdBy, CancellationToken cancellationToken = default)
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

            // Add Audit Log
            _ = _context.AuditLogs.Add(new AuditLog
            {
                OrderId = order.OrderId,
                Action = "OrderCreated",
                PerformedBy = createdBy,
                PerformedAt = DateTime.UtcNow,
                EntityType = "Order",
                EntityId = order.OrderId
            });

            return order;
        }

        /// <inheritdoc />
        public async Task<OrderResponse> UpdateOrderAsync(string orderId, UpdateOrderRequest request, string updatedBy, CancellationToken cancellationToken = default)
        {
            Order? order = await _context.Orders.FindAsync([orderId], cancellationToken) ?? throw new InvalidOperationException($"Order {orderId} not found");

            // Optimistic concurrency check using xmin shadow property
            if (!uint.TryParse(request.Version, out uint requestVersion))
            {
                throw new InvalidOperationException($"Invalid version format for order {orderId}. Version must be a valid number.");
            }

            uint? currentVersion = _context.Entry(order).Property<uint>("xmin").CurrentValue;
            if (currentVersion != requestVersion)
            {
                throw new DbUpdateConcurrencyException("Order has been modified by another user");
            }

            // Capture state for audit
            string oldState = System.Text.Json.JsonSerializer.Serialize(order);

            order.UpdateOrder(request);
            order.UpdatedBy = updatedBy;
            order.UpdatedAt = DateTime.UtcNow;

            // Add Audit Log
            _ = _context.AuditLogs.Add(new AuditLog
            {
                OrderId = order.OrderId,
                Action = "OrderUpdated",
                PerformedBy = updatedBy,
                PerformedAt = DateTime.UtcNow,
                EntityType = "Order",
                EntityId = order.OrderId,
                ChangeDetails = System.Text.Json.JsonSerializer.Serialize(new { before = oldState })
            });

            _ = await _context.SaveChangesAsync(cancellationToken);

            uint? xmin = _context.Entry(order).Property<uint>("xmin").CurrentValue;
            return order.ToOrderResponse(xmin);
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

            // Add Audit Log
            _ = _context.AuditLogs.Add(new AuditLog
            {
                OrderId = orderId,
                Action = "OrderCancelled",
                PerformedBy = cancelledBy,
                PerformedAt = DateTime.UtcNow,
                EntityType = "Order",
                EntityId = orderId,
                ChangeDetails = System.Text.Json.JsonSerializer.Serialize(new { reason })
            });

            _ = await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        private async Task<string> GenerateOrderIdAsync(CancellationToken cancellationToken)
        {
            int year = DateTime.UtcNow.Year;
            string prefix = $"ORD-{year}-";

            // Use PostgreSQL sequence for atomic ID generation
            long nextVal = await _context.Database.SqlQueryRaw<long>("SELECT nextval('order_id_seq') AS \"Value\"").SingleAsync(cancellationToken);

            return $"{prefix}{nextVal:D5}";
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
