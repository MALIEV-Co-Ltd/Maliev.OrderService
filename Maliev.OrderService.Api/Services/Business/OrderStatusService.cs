using Maliev.MessagingContracts.Contracts.Orders;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Api.Mapping;
using Maliev.OrderService.Infrastructure.Persistence;
using Maliev.OrderService.Domain.Entities;
using Maliev.OrderService.Domain.Enums;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

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
    /// <param name="publishEndpoint">The MassTransit publish endpoint</param>
    public partial class OrderStatusService(
        OrderDbContext context,
        ILogger<OrderStatusService> logger,
        IPublishEndpoint publishEndpoint) : IOrderStatusService
    {
        private readonly OrderDbContext _context = context;
        private readonly ILogger<OrderStatusService> _logger = logger;
        private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;

        // Static readonly arrays for ConsumedBy lists to avoid CA1861
        private static readonly List<string> _genericStatusChangeConsumers = ["NotificationService", "InvoiceService"];
        private static readonly List<string> _quotedConsumers = ["NotificationService", "InvoiceService"];
        private static readonly List<string> _acceptedConsumers = ["PaymentService", "NotificationService"];
        private static readonly List<string> _paidConsumers = ["InvoiceService", "NotificationService"];
        private static readonly List<string> _notificationOnlyConsumers = ["NotificationService"];
        private static readonly List<string> _completedConsumers = ["NotificationService", "InvoiceService"];
        private static readonly List<string> _cancelledConsumers = ["PaymentService", "NotificationService"];

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
            Order? order = await _context.Orders.FindAsync([orderId], cancellationToken)
                ?? throw new InvalidOperationException($"Order {orderId} not found");

            // Get current status
            OrderStatus? currentStatus = await _context.OrderStatuses
                .Where(s => s.OrderId == orderId)
                .OrderByDescending(s => s.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);

            OrderStatusValue fromStatus = currentStatus != null
                ? Enum.Parse<OrderStatusValue>(currentStatus.Status)
                : OrderStatusValue.New;

            if (!Enum.TryParse(request.Status, out OrderStatusValue toStatus))
            {
                throw new InvalidOperationException($"Invalid status value: {request.Status}");
            }

            // Full state machine validation
            if (!IsValidTransition(fromStatus, toStatus))
            {
                throw new InvalidOperationException($"Invalid transition from {fromStatus} to {toStatus}");
            }

            var newStatus = request.ToOrderStatus();
            newStatus.OrderId = orderId;
            newStatus.UpdatedBy = updatedBy;
            newStatus.Timestamp = DateTime.UtcNow;

            _ = _context.OrderStatuses.Add(newStatus);

            // Add Audit Log
            _ = _context.AuditLogs.Add(new AuditLog
            {
                OrderId = orderId,
                Action = "StatusChanged",
                PerformedBy = updatedBy,
                PerformedAt = DateTime.UtcNow,
                EntityType = "OrderStatus",
                EntityId = orderId,
                ChangeDetails = System.Text.Json.JsonSerializer.Serialize(new { from = fromStatus.ToString(), to = toStatus.ToString(), notes = request.CustomerNotes })
            });

            // Publish status change events via Transactional Outbox (implicitly handled by SaveChanges if configured)
            await PublishStatusChangeEventsAsync(
                order,
                fromStatus,
                toStatus,
                updatedBy,
                request.InternalNotes,
                cancellationToken);

            _ = await _context.SaveChangesAsync(cancellationToken);

            return newStatus.ToOrderStatusResponse();
        }

        private static bool IsValidTransition(OrderStatusValue from, OrderStatusValue to)
        {
            return from != to && from switch
            {
                OrderStatusValue.New => to is OrderStatusValue.Reviewing or OrderStatusValue.Cancelled,
                OrderStatusValue.Reviewing => to is OrderStatusValue.Rejected or OrderStatusValue.Reviewed or OrderStatusValue.Cancelled,
                OrderStatusValue.Reviewed => to is OrderStatusValue.Quoted or OrderStatusValue.Cancelled,
                OrderStatusValue.Quoted => to is OrderStatusValue.Declined or OrderStatusValue.Accepted or OrderStatusValue.Expired or OrderStatusValue.Cancelled,
                OrderStatusValue.Accepted => to is OrderStatusValue.Paid or OrderStatusValue.POIssued or OrderStatusValue.Cancelled,
                OrderStatusValue.Paid => to is OrderStatusValue.InProgress or OrderStatusValue.Cancelled,
                OrderStatusValue.POIssued => to is OrderStatusValue.InProgress or OrderStatusValue.Cancelled,
                OrderStatusValue.InProgress => to is OrderStatusValue.OnHold or OrderStatusValue.Finished or OrderStatusValue.Cancelled,
                OrderStatusValue.OnHold => to is OrderStatusValue.InProgress or OrderStatusValue.Cancelled,
                OrderStatusValue.Finished => to is OrderStatusValue.Shipped or OrderStatusValue.Reopen,
                OrderStatusValue.Shipped => to is OrderStatusValue.Reopen,
                OrderStatusValue.Reopen => to is OrderStatusValue.InProgress,
                OrderStatusValue.Rejected or OrderStatusValue.Declined or OrderStatusValue.Expired or OrderStatusValue.Cancelled => false,
                _ => false
            };
        }

        /// <summary>
        /// Publishes appropriate events based on order status transition
        /// </summary>
        private async Task PublishStatusChangeEventsAsync(
            Order order,
            OrderStatusValue previousStatus,
            OrderStatusValue newStatus,
            string changedBy,
            string? reason,
            CancellationToken cancellationToken)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Guid orderGuid = StringToGuid(order.OrderId);
            Guid customerGuid = StringToGuid(order.CustomerId);

            // Always publish generic OrderStatusChangedEvent
            await _publishEndpoint.Publish(new OrderStatusChangedEvent(
                MessageId: Guid.NewGuid(),
                MessageName: nameof(OrderStatusChangedEvent),
                MessageType: MessageType.Event,
                MessageVersion: "1.0.0",
                PublishedBy: "OrderService",
                ConsumedBy: _genericStatusChangeConsumers,
                CorrelationId: Guid.NewGuid(),
                CausationId: null,
                OccurredAtUtc: now,
                IsPublic: false,
                Payload: new OrderStatusChangedEventPayload(
                    OrderId: orderGuid,
                    OrderNumber: order.OrderId,
                    PreviousStatus: previousStatus.ToString(),
                    NewStatus: newStatus.ToString(),
                    ChangedBy: StringToGuid(changedBy),
                    ChangedAt: now,
                    Reason: reason
                )
            ), cancellationToken);

            // Publish specific events based on new status
            switch (newStatus)
            {
                case OrderStatusValue.Quoted:
                    await _publishEndpoint.Publish(new OrderQuotedEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: nameof(OrderQuotedEvent),
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: _quotedConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: new OrderQuotedEventPayload(
                            OrderId: orderGuid,
                            OrderNumber: order.OrderId,
                            QuotedAmount: (double)(order.QuotedAmount ?? 0),
                            Currency: order.QuoteCurrency ?? "THB",
                            ValidUntil: now.AddDays(30),
                            QuotedBy: StringToGuid(changedBy),
                            QuotedAt: now
                        )
                    ), cancellationToken);
                    break;

                case OrderStatusValue.Accepted:
                    await _publishEndpoint.Publish(new OrderAcceptedEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: nameof(OrderAcceptedEvent),
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: _acceptedConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: new OrderAcceptedEventPayload(
                            OrderId: orderGuid,
                            OrderNumber: order.OrderId,
                            CustomerId: customerGuid,
                            AcceptedAmount: (double)(order.QuotedAmount ?? 0),
                            Currency: order.QuoteCurrency ?? "THB",
                            AcceptedAt: now
                        )
                    ), cancellationToken);
                    break;

                case OrderStatusValue.Paid:
                    await _publishEndpoint.Publish(new OrderPaidEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: nameof(OrderPaidEvent),
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: _paidConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: new OrderPaidEventPayload(
                            OrderId: orderGuid,
                            OrderNumber: order.OrderId,
                            PaymentId: order.PaymentId != null ? StringToGuid(order.PaymentId) : Guid.Empty,
                            PaidAmount: (double)(order.QuotedAmount ?? 0),
                            Currency: order.QuoteCurrency ?? "THB",
                            PaidAt: now
                        )
                    ), cancellationToken);
                    break;

                case OrderStatusValue.InProgress:
                    await _publishEndpoint.Publish(new OrderInProgressEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: nameof(OrderInProgressEvent),
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: _notificationOnlyConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: new OrderInProgressEventPayload(
                            OrderId: orderGuid,
                            OrderNumber: order.OrderId,
                            StartedAt: now,
                            EstimatedCompletionDate: order.PromisedDeliveryDate != null
                                ? new DateTimeOffset(order.PromisedDeliveryDate.Value, TimeSpan.Zero)
                                : null
                        )
                    ), cancellationToken);
                    break;

                case OrderStatusValue.Finished:
                    await _publishEndpoint.Publish(new OrderCompletedEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: nameof(OrderCompletedEvent),
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: _completedConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: new OrderCompletedEventPayload(
                            OrderId: orderGuid,
                            OrderNumber: order.OrderId,
                            QuotationId: Guid.Empty,
                            OrderCreatedAt: new DateTimeOffset(order.CreatedAt, TimeSpan.Zero),
                            CompletedAt: now,
                            CompletedBy: StringToGuid(changedBy),
                            JobSucceeded: true,
                            ActualMaterialUsedCm3: null,
                            ActualPrintTimeHours: null,
                            ActualLaborHours: null,
                            ActualTotalCost: null,
                            Items: [
                                new OrderCompletedEventPayloadItemsItem(
                                    ProductId: order.MaterialId.HasValue ? StringToGuid(order.MaterialId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)) : Guid.Empty,
                                    ProductCode: order.MaterialName ?? "Unknown",
                                    ProductName: order.MaterialName ?? "Unknown",
                                    Quantity: Convert.ToDouble(order.OrderedQuantity ?? 1),
                                    UnitPrice: Convert.ToDouble(order.QuotedAmount ?? 0) / Convert.ToDouble(order.OrderedQuantity ?? 1),
                                    LineTotal: Convert.ToDouble(order.QuotedAmount ?? 0)
                                )
                            ]
                        )
                    ), cancellationToken);
                    break;

                case OrderStatusValue.Shipped:
                    await _publishEndpoint.Publish(new OrderShippedEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: nameof(OrderShippedEvent),
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: _notificationOnlyConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: new OrderShippedEventPayload(
                            OrderId: orderGuid,
                            OrderNumber: order.OrderId,
                            ShippedAt: now,
                            TrackingNumber: null,
                            Carrier: null,
                            EstimatedDeliveryDate: null
                        )
                    ), cancellationToken);
                    break;

                case OrderStatusValue.Cancelled:
                    await _publishEndpoint.Publish(new OrderCancelledEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: nameof(OrderCancelledEvent),
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: _cancelledConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: new OrderCancelledEventPayload(
                            OrderId: orderGuid,
                            OrderNumber: order.OrderId,
                            CancelledBy: StringToGuid(changedBy),
                            CancelledAt: now,
                            CancellationReason: reason ?? "Not specified",
                            RefundRequired: previousStatus == OrderStatusValue.Paid
                        )
                    ), cancellationToken);
                    break;

                case OrderStatusValue.Rejected:
                    await _publishEndpoint.Publish(new OrderRejectedEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: nameof(OrderRejectedEvent),
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: _notificationOnlyConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: new OrderRejectedEventPayload(
                            OrderId: orderGuid,
                            OrderNumber: order.OrderId,
                            RejectedBy: StringToGuid(changedBy),
                            RejectedAt: now,
                            RejectionReason: reason ?? "Not specified"
                        )
                    ), cancellationToken);
                    break;

                case OrderStatusValue.OnHold:
                    await _publishEndpoint.Publish(new OrderOnHoldEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: nameof(OrderOnHoldEvent),
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: _notificationOnlyConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: new OrderOnHoldEventPayload(
                            OrderId: orderGuid,
                            OrderNumber: order.OrderId,
                            PutOnHoldBy: StringToGuid(changedBy),
                            PutOnHoldAt: now,
                            HoldReason: reason ?? "Not specified"
                        )
                    ), cancellationToken);
                    break;

                case OrderStatusValue.Reopen:
                    await _publishEndpoint.Publish(new OrderReopenedEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: nameof(OrderReopenedEvent),
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: _notificationOnlyConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: new OrderReopenedEventPayload(
                            OrderId: orderGuid,
                            OrderNumber: order.OrderId,
                            ReopenedBy: StringToGuid(changedBy),
                            ReopenedAt: now,
                            ReopenReason: reason ?? "Not specified"
                        )
                    ), cancellationToken);
                    break;
                case OrderStatusValue.New:
                    break;
                case OrderStatusValue.Reviewing:
                    break;
                case OrderStatusValue.Reviewed:
                    break;
                case OrderStatusValue.Declined:
                    break;
                case OrderStatusValue.Expired:
                    break;
                case OrderStatusValue.POIssued:
                    break;
                default:
                    break;
            }

            string prevStatusStr = previousStatus.ToString();
            string newStatusStr = newStatus.ToString();
            Log.StatusChangeEventsPublished(_logger, order.OrderId, prevStatusStr, newStatusStr);
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

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Information, Message = "Published status change events for order {OrderId}: {PreviousStatus} → {NewStatus}")]
            public static partial void StatusChangeEventsPublished(ILogger logger, string orderId, string previousStatus, string newStatus);
        }
    }
}
