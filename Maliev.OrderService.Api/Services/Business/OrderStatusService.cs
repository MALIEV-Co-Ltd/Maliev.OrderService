using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Api.Mapping;
using Maliev.OrderService.Data;
using Maliev.OrderService.Data.Models;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using Maliev.MessagingContracts.Generated;
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
        private static readonly string[] GenericStatusChangeConsumers = ["NotificationService", "InvoiceService"];
        private static readonly string[] QuotedConsumers = ["NotificationService", "InvoiceService"];
        private static readonly string[] AcceptedConsumers = ["PaymentService", "NotificationService"];
        private static readonly string[] PaidConsumers = ["InvoiceService", "NotificationService"];
        private static readonly string[] NotificationOnlyConsumers = ["NotificationService"];
        private static readonly string[] CompletedConsumers = ["NotificationService", "InvoiceService"];
        private static readonly string[] CancelledConsumers = ["PaymentService", "NotificationService"];

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
            bool orderExists = await _context.Orders.AnyAsync(o => o.OrderId == orderId, cancellationToken);
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

            // Get order details for event publishing
            Order? order = await _context.Orders.FindAsync([orderId], cancellationToken)
                ?? throw new InvalidOperationException($"Order {orderId} not found after status update");

            // Publish status change events
            await PublishStatusChangeEventsAsync(
                order,
                currentStatus?.Status,
                request.Status,
                updatedBy,
                request.InternalNotes,
                cancellationToken);

            return newStatus.ToOrderStatusResponse();
        }

        /// <summary>
        /// Publishes appropriate events based on order status transition
        /// </summary>
        private async Task PublishStatusChangeEventsAsync(
            Order order,
            string? previousStatus,
            string newStatus,
            string changedBy,
            string? reason,
            CancellationToken cancellationToken)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Guid orderId = StringToGuid(order.OrderId);
            Guid customerId = StringToGuid(order.CustomerId);

            // Always publish generic OrderStatusChangedEvent
            await _publishEndpoint.Publish(new OrderStatusChangedEvent(
                MessageId: Guid.NewGuid(),
                MessageName: "OrderStatusChangedEvent",
                MessageType: MessageType.Event,
                MessageVersion: "1.0.0",
                PublishedBy: "OrderService",
                ConsumedBy: GenericStatusChangeConsumers,
                CorrelationId: Guid.NewGuid(),
                CausationId: null,
                OccurredAtUtc: now,
                IsPublic: false,
                Payload: new OrderStatusChangedEventPayload(
                    OrderId: orderId,
                    OrderNumber: order.OrderId,
                    PreviousStatus: previousStatus ?? "None",
                    NewStatus: newStatus,
                    ChangedBy: StringToGuid(changedBy),
                    ChangedAt: now,
                    Reason: reason
                )
            ), cancellationToken);

            // Publish specific events based on new status
            switch (newStatus)
            {
                case "Quoted":
                    // Get payload requirements from schema
                    var quotedPayload = new OrderQuotedEventPayload(
                        OrderId: orderId,
                        OrderNumber: order.OrderId,
                        QuotedAmount: (double)(order.QuotedAmount ?? 0),
                        Currency: order.QuoteCurrency ?? "THB",
                        ValidUntil: now.AddDays(30),
                        QuotedBy: StringToGuid(changedBy),
                        QuotedAt: now
                    );

                    await _publishEndpoint.Publish(new OrderQuotedEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: "OrderQuotedEvent",
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: QuotedConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: quotedPayload
                    ), cancellationToken);
                    break;

                case "Accepted":
                    await _publishEndpoint.Publish(new OrderAcceptedEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: "OrderAcceptedEvent",
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: AcceptedConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: new OrderAcceptedEventPayload(
                            OrderId: orderId,
                            OrderNumber: order.OrderId,
                            CustomerId: customerId,
                            AcceptedAmount: (double)(order.QuotedAmount ?? 0),
                            Currency: order.QuoteCurrency ?? "THB",
                            AcceptedAt: now
                        )
                    ), cancellationToken);
                    break;

                case "Paid":
                    await _publishEndpoint.Publish(new OrderPaidEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: "OrderPaidEvent",
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: PaidConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: new OrderPaidEventPayload(
                            OrderId: orderId,
                            OrderNumber: order.OrderId,
                            PaymentId: order.PaymentId != null ? StringToGuid(order.PaymentId) : Guid.Empty,
                            PaidAmount: (double)(order.QuotedAmount ?? 0),
                            Currency: order.QuoteCurrency ?? "THB",
                            PaidAt: now
                        )
                    ), cancellationToken);
                    break;

                case "InProgress":
                    await _publishEndpoint.Publish(new OrderInProgressEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: "OrderInProgressEvent",
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: NotificationOnlyConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: new OrderInProgressEventPayload(
                            OrderId: orderId,
                            OrderNumber: order.OrderId,
                            StartedAt: now,
                            EstimatedCompletionDate: order.PromisedDeliveryDate != null
                                ? new DateTimeOffset(order.PromisedDeliveryDate.Value, TimeSpan.Zero)
                                : null
                        )
                    ), cancellationToken);
                    break;

                case "Finished":
                    await _publishEndpoint.Publish(new OrderCompletedEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: "OrderCompletedEvent",
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: CompletedConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: new OrderCompletedEventPayload(
                            OrderId: orderId,
                            OrderNumber: order.OrderId,
                            CompletedAt: now,
                            CompletedBy: StringToGuid(changedBy)
                        )
                    ), cancellationToken);
                    break;

                case "Shipped":
                    await _publishEndpoint.Publish(new OrderShippedEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: "OrderShippedEvent",
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: NotificationOnlyConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: new OrderShippedEventPayload(
                            OrderId: orderId,
                            OrderNumber: order.OrderId,
                            ShippedAt: now,
                            TrackingNumber: null, // TODO: Get from order tracking data
                            Carrier: null,
                            EstimatedDeliveryDate: null
                        )
                    ), cancellationToken);
                    break;

                case "Cancelled":
                    await _publishEndpoint.Publish(new OrderCancelledEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: "OrderCancelledEvent",
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: CancelledConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: new OrderCancelledEventPayload(
                            OrderId: orderId,
                            OrderNumber: order.OrderId,
                            CancelledBy: StringToGuid(changedBy),
                            CancelledAt: now,
                            CancellationReason: reason ?? "Not specified",
                            RefundRequired: previousStatus == "Paid" // Refund if already paid
                        )
                    ), cancellationToken);
                    break;

                case "Rejected":
                    await _publishEndpoint.Publish(new OrderRejectedEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: "OrderRejectedEvent",
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: NotificationOnlyConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: new OrderRejectedEventPayload(
                            OrderId: orderId,
                            OrderNumber: order.OrderId,
                            RejectedBy: StringToGuid(changedBy),
                            RejectedAt: now,
                            RejectionReason: reason ?? "Not specified"
                        )
                    ), cancellationToken);
                    break;

                case "OnHold":
                    await _publishEndpoint.Publish(new OrderOnHoldEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: "OrderOnHoldEvent",
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: NotificationOnlyConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: new OrderOnHoldEventPayload(
                            OrderId: orderId,
                            OrderNumber: order.OrderId,
                            PutOnHoldBy: StringToGuid(changedBy),
                            PutOnHoldAt: now,
                            HoldReason: reason ?? "Not specified"
                        )
                    ), cancellationToken);
                    break;

                case "Reopened":
                    await _publishEndpoint.Publish(new OrderReopenedEvent(
                        MessageId: Guid.NewGuid(),
                        MessageName: "OrderReopenedEvent",
                        MessageType: MessageType.Event,
                        MessageVersion: "1.0.0",
                        PublishedBy: "OrderService",
                        ConsumedBy: NotificationOnlyConsumers,
                        CorrelationId: Guid.NewGuid(),
                        CausationId: null,
                        OccurredAtUtc: now,
                        IsPublic: false,
                        Payload: new OrderReopenedEventPayload(
                            OrderId: orderId,
                            OrderNumber: order.OrderId,
                            ReopenedBy: StringToGuid(changedBy),
                            ReopenedAt: now,
                            ReopenReason: reason ?? "Not specified"
                        )
                    ), cancellationToken);
                    break;
                default:
                    break;
            }

            Log.StatusChangeEventsPublished(_logger, order.OrderId, previousStatus, newStatus);
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
            public static partial void StatusChangeEventsPublished(ILogger logger, string orderId, string? previousStatus, string newStatus);
        }
    }
}
