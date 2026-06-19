using Maliev.MessagingContracts.Contracts.Orders;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Api.Mapping;
using Maliev.OrderService.Infrastructure.Persistence;
using Maliev.OrderService.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Maliev.OrderService.Api.Services.Business
{
    /// <summary>
    /// Service for managing orders
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="OrderManagementService"/> class.
    /// </remarks>
    /// <param name="context">The database context</param>
    /// <param name="authService">The order authorization service</param>
    /// <param name="logger">The logger instance</param>
    /// <param name="publishEndpoint">The MassTransit publish endpoint</param>
    public partial class OrderManagementService(
        OrderDbContext context,
        IOrderAuthorizationService authService,
        ILogger<OrderManagementService> logger,
        IPublishEndpoint publishEndpoint) : IOrderManagementService
    {
        private readonly OrderDbContext _context = context;
        private readonly IOrderAuthorizationService _authService = authService;
        private readonly ILogger<OrderManagementService> _logger = logger;
        private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;
        private static readonly System.Text.Json.JsonSerializerOptions _productionItemJsonOptions =
            new(System.Text.Json.JsonSerializerDefaults.Web);

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
            System.Security.Claims.ClaimsPrincipal user,
            string? customerId = null,
            string? status = null,
            string? search = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Order> query = _context.Orders
                .Include(o => o.ServiceCategory)
                .Include(o => o.ProcessType)
                .Include(o => o.OrderStatuses)
                .AsQueryable();

            // Apply data isolation filter based on user roles
            query = _authService.ApplyDataIsolationFilter(user, query);

            if (!string.IsNullOrEmpty(customerId))
            {
                query = query.Where(o => o.CustomerId == customerId);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.OrderStatuses.OrderByDescending(s => s.Timestamp).Select(s => s.Status).FirstOrDefault() == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();
                query = query.Where(o =>
                    o.OrderId.Contains(term) ||
                    (o.CustomerId != null && o.CustomerId.Contains(term)) ||
                    (o.BillingCompanyName != null && o.BillingCompanyName.Contains(term)) ||
                    (o.DeliveryContactName != null && o.DeliveryContactName.Contains(term)));
            }

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

            await _publishEndpoint.Publish(new OrderCreatedEvent(
                MessageId: Guid.NewGuid(),
                MessageName: nameof(OrderCreatedEvent),
                MessageType: MessageType.Event,
                MessageVersion: "1.0.0",
                PublishedBy: "OrderService",
                ConsumedBy: _orderCreatedConsumers,
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
                    AssignedEmployeeId: order.AssignedEmployeeId != null ? StringToGuid(order.AssignedEmployeeId) : null,
                    Items: BuildOrderCreatedItems(order)
                )
            ), cancellationToken);

            _ = await _context.SaveChangesAsync(cancellationToken);

            uint? xmin = _context.Entry(order).Property<uint>("xmin").CurrentValue;
            var response = order.ToOrderResponse(xmin);

            Log.OrderCreatedEventPublished(_logger, order.OrderId);

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

        /// <inheritdoc />
        public async Task<OrderResponse> UpdateOutsourcingAsync(string orderId, UpdateOutsourcingRequest request, string actorId, CancellationToken cancellationToken = default)
        {
            Order? order = await _context.Orders.FindAsync([orderId], cancellationToken) ?? throw new InvalidOperationException($"Order {orderId} not found");

            order.IsOutsourced = request.IsOutsourced;
            order.SupplierCostTHB = request.SupplierCostTHB;
            order.SupplierName = request.SupplierName;
            order.SupplierEstimatedDelivery = request.SupplierEstimatedDelivery;
            order.UpdatedBy = actorId;
            order.UpdatedAt = DateTime.UtcNow;

            _ = _context.AuditLogs.Add(new AuditLog
            {
                OrderId = order.OrderId,
                Action = "OrderOutsourcingChanged",
                PerformedBy = actorId,
                PerformedAt = DateTime.UtcNow,
                EntityType = "Order",
                EntityId = order.OrderId,
                ChangeDetails = System.Text.Json.JsonSerializer.Serialize(new { request.IsOutsourced, request.SupplierCostTHB, request.SupplierName, request.SupplierEstimatedDelivery })
            });

            await _publishEndpoint.Publish(new OrderOutsourcingChangedEvent(
                MessageId: Guid.NewGuid(),
                MessageName: nameof(OrderOutsourcingChangedEvent),
                MessageType: MessageType.Event,
                MessageVersion: "1.0.0",
                PublishedBy: "OrderService",
                ConsumedBy: _outsourcingChangedConsumers,
                CorrelationId: Guid.NewGuid(),
                CausationId: null,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                IsPublic: false,
                Payload: new OrderOutsourcingChangedEventPayload(
                    OrderId: StringToGuid(order.OrderId),
                    IsOutsourced: order.IsOutsourced,
                    SupplierName: order.SupplierName,
                    SupplierCostTHB: (double?)order.SupplierCostTHB,
                    ChangedBy: StringToGuid(actorId),
                    ChangedAtUtc: DateTimeOffset.UtcNow
                )
            ), cancellationToken);

            _ = await _context.SaveChangesAsync(cancellationToken);

            Log.OutsourcingChangedEventPublished(_logger, orderId);

            uint? xmin = _context.Entry(order).Property<uint>("xmin").CurrentValue;
            return order.ToOrderResponse(xmin);
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
        /// Converts a string ID to a stable Guid, preserving GUID strings when supplied.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "MD5 is used for deterministic hashing, not cryptography")]
        private static Guid StringToGuid(string value)
        {
            if (Guid.TryParse(value, out Guid parsedGuid))
            {
                return parsedGuid;
            }

            byte[] hash = MD5.HashData(System.Text.Encoding.UTF8.GetBytes(value));
            return new Guid(hash);
        }

        private static IReadOnlyList<OrderCreatedEventPayloadItemsItem> BuildOrderCreatedItems(Order order)
        {
            if (!string.IsNullOrWhiteSpace(order.ProductionItemsJson))
            {
                List<CreateOrderProductionItemRequest>? productionItems = System.Text.Json.JsonSerializer.Deserialize<List<CreateOrderProductionItemRequest>>(
                    order.ProductionItemsJson,
                    _productionItemJsonOptions);
                if (productionItems is { Count: > 0 })
                {
                    return productionItems.Select((item, index) =>
                    {
                        var quantity = Math.Max(1, item.Quantity);
                        var lineTotal = CalculateLineTotal(order, quantity, productionItems.Count);
                        return new OrderCreatedEventPayloadItemsItem(
                            ProductId: StringToGuid($"order-item:{order.OrderId}:{item.SourceProjectPartId?.ToString("D") ?? index.ToString(System.Globalization.CultureInfo.InvariantCulture)}"),
                            ProductCode: item.MaterialId.ToString("D"),
                            ProductName: item.Technology,
                            SourceProjectId: item.SourceProjectId,
                            SourceProjectPartId: item.SourceProjectPartId,
                            Quantity: quantity,
                            UnitPrice: lineTotal / quantity,
                            LineTotal: lineTotal);
                    }).ToArray();
                }
            }

            var fallbackQuantity = Math.Max(1, order.OrderedQuantity ?? 1);
            var fallbackLineTotal = (double)(order.QuotedAmount ?? 0);
            return
            [
                new OrderCreatedEventPayloadItemsItem(
                    ProductId: order.MaterialId.HasValue ? StringToGuid(order.MaterialId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)) : Guid.Empty,
                    ProductCode: order.MaterialName ?? "Unknown",
                    ProductName: order.MaterialName ?? "Unknown",
                    SourceProjectId: null,
                    SourceProjectPartId: null,
                    Quantity: fallbackQuantity,
                    UnitPrice: fallbackLineTotal / fallbackQuantity,
                    LineTotal: fallbackLineTotal)
            ];
        }

        private static double CalculateLineTotal(Order order, int itemQuantity, int itemCount)
        {
            var total = (double)(order.QuotedAmount ?? 0);
            if (total <= 0)
            {
                return 0;
            }

            var orderedQuantity = Math.Max(1, order.OrderedQuantity ?? itemQuantity);
            return Math.Round(total * itemQuantity / orderedQuantity, 2);
        }

        private static readonly List<string> _orderCreatedConsumers = ["InvoiceService", "NotificationService", "ProjectService"];
        private static readonly List<string> _outsourcingChangedConsumers = ["JobService", "NotificationService"];

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Warning, Message = "Race condition detected during OrderId generation. Retrying {RetryCount}/{MaxRetries}...")]
            public static partial void RaceConditionDetected(ILogger logger, int retryCount, int maxRetries);

            [LoggerMessage(Level = LogLevel.Information, Message = "Published OrderCreatedEvent for order {OrderId}")]
            public static partial void OrderCreatedEventPublished(ILogger logger, string orderId);

            [LoggerMessage(Level = LogLevel.Information, Message = "Published OrderOutsourcingChangedEvent for order {OrderId}")]
            public static partial void OutsourcingChangedEventPublished(ILogger logger, string orderId);
        }
    }
}
