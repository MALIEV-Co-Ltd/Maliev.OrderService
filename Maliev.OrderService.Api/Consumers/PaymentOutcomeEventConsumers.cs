using Maliev.MessagingContracts.Contracts.Payments;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.Services.Business;
using MassTransit;

namespace Maliev.OrderService.Api.Consumers
{
    /// <summary>
    /// Consumer for payment pending events that record provider checkout processing state.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="PaymentPendingEventConsumer"/> class.
    /// </remarks>
    /// <param name="orderStatusService">The order status service.</param>
    /// <param name="logger">The logger instance.</param>
    public sealed partial class PaymentPendingEventConsumer(
        IOrderStatusService orderStatusService,
        ILogger<PaymentPendingEventConsumer> logger) : IConsumer<PaymentPendingEvent>
    {
        private readonly IOrderStatusService _orderStatusService = orderStatusService;
        private readonly ILogger<PaymentPendingEventConsumer> _logger = logger;

        /// <summary>
        /// Consumes a payment pending event and records payment processing state when routed here.
        /// </summary>
        /// <param name="context">The message context containing the payment pending event.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task Consume(ConsumeContext<PaymentPendingEvent> context)
        {
            PaymentPendingEvent message = context.Message;
            PaymentPendingEventPayload? payload = message.Payload;

            if (payload is null)
            {
                Log.PaymentPendingEventPayloadMissing(_logger);
                return;
            }

            if (!PaymentOutcomeConsumerHelper.IsRoutedToOrderService(message.ConsumedBy))
            {
                Log.IgnoringUntargetedPaymentPendingEvent(_logger, payload.OrderId, payload.TransactionId);
                return;
            }

            await _orderStatusService.MarkPaymentProcessingAsync(
                payload.OrderId,
                payload.TransactionId.ToString(),
                payload.ProviderName,
                payload.ProviderEventCode,
                "System-PaymentService",
                context.CancellationToken);
        }

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Warning, Message = "PaymentPendingEvent received without payload; skipping")]
            public static partial void PaymentPendingEventPayloadMissing(ILogger logger);

            [LoggerMessage(Level = LogLevel.Debug, Message = "Ignoring untargeted PaymentPendingEvent for order {OrderId}, transaction {TransactionId}")]
            public static partial void IgnoringUntargetedPaymentPendingEvent(ILogger logger, string orderId, Guid transactionId);
        }
    }

    /// <summary>
    /// Consumer for payment cancellation events that cancel the associated accepted order.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="PaymentCancelledEventConsumer"/> class.
    /// </remarks>
    /// <param name="orderStatusService">The order status service.</param>
    /// <param name="logger">The logger instance.</param>
    public sealed partial class PaymentCancelledEventConsumer(
        IOrderStatusService orderStatusService,
        ILogger<PaymentCancelledEventConsumer> logger) : IConsumer<PaymentCancelledEvent>
    {
        private readonly IOrderStatusService _orderStatusService = orderStatusService;
        private readonly ILogger<PaymentCancelledEventConsumer> _logger = logger;

        /// <summary>
        /// Consumes a payment cancellation event and cancels the associated order when routed here.
        /// </summary>
        /// <param name="context">The message context containing the payment cancellation event.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task Consume(ConsumeContext<PaymentCancelledEvent> context)
        {
            PaymentCancelledEvent message = context.Message;
            PaymentCancelledEventPayload? payload = message.Payload;

            if (payload is null)
            {
                Log.PaymentCancelledEventPayloadMissing(_logger);
                return;
            }

            if (!PaymentOutcomeConsumerHelper.IsRoutedToOrderService(message.ConsumedBy))
            {
                Log.IgnoringUntargetedPaymentCancelledEvent(_logger, payload.OrderId, payload.TransactionId);
                return;
            }

            var request = new CreateOrderStatusRequest
            {
                Status = "Cancelled",
                InternalNotes = $"Payment {payload.TransactionId} cancelled - {payload.Reason}",
                CustomerNotes = "Payment was cancelled before completion."
            };

            try
            {
                _ = await _orderStatusService.CreateOrderStatusAsync(
                    payload.OrderId,
                    request,
                    "System-PaymentService",
                    context.CancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                if (await PaymentOutcomeConsumerHelper.IsDuplicateCancelledOutcomeAsync(
                        _orderStatusService,
                        payload.OrderId,
                        payload.TransactionId,
                        context.CancellationToken))
                {
                    Log.DuplicatePaymentCancelledEventIgnored(_logger, payload.OrderId, payload.TransactionId);
                    return;
                }

                Log.FailedToCancelOrderAfterPaymentCancellation(_logger, ex, payload.OrderId, ex.Message);
                throw;
            }
        }

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Warning, Message = "PaymentCancelledEvent received without payload; skipping")]
            public static partial void PaymentCancelledEventPayloadMissing(ILogger logger);

            [LoggerMessage(Level = LogLevel.Debug, Message = "Ignoring untargeted PaymentCancelledEvent for order {OrderId}, transaction {TransactionId}")]
            public static partial void IgnoringUntargetedPaymentCancelledEvent(ILogger logger, string orderId, Guid transactionId);

            [LoggerMessage(Level = LogLevel.Information, Message = "Ignoring duplicate PaymentCancelledEvent for order {OrderId}, transaction {TransactionId}")]
            public static partial void DuplicatePaymentCancelledEventIgnored(ILogger logger, string orderId, Guid transactionId);

            [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to cancel order {OrderId} after payment cancellation: {Message}")]
            public static partial void FailedToCancelOrderAfterPaymentCancellation(ILogger logger, Exception exception, string orderId, string message);
        }
    }

    /// <summary>
    /// Consumer for payment failure events that cancel the associated accepted order.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="PaymentFailedEventConsumer"/> class.
    /// </remarks>
    /// <param name="orderStatusService">The order status service.</param>
    /// <param name="logger">The logger instance.</param>
    public sealed partial class PaymentFailedEventConsumer(
        IOrderStatusService orderStatusService,
        ILogger<PaymentFailedEventConsumer> logger) : IConsumer<PaymentFailedEvent>
    {
        private readonly IOrderStatusService _orderStatusService = orderStatusService;
        private readonly ILogger<PaymentFailedEventConsumer> _logger = logger;

        /// <summary>
        /// Consumes a payment failure event and cancels the associated order when routed here.
        /// </summary>
        /// <param name="context">The message context containing the payment failure event.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
        {
            PaymentFailedEvent message = context.Message;
            PaymentFailedEventPayload? payload = message.Payload;

            if (payload is null)
            {
                Log.PaymentFailedEventPayloadMissing(_logger);
                return;
            }

            if (!PaymentOutcomeConsumerHelper.IsRoutedToOrderService(message.ConsumedBy))
            {
                Log.IgnoringUntargetedPaymentFailedEvent(_logger, payload.OrderId, payload.TransactionId);
                return;
            }

            var request = new CreateOrderStatusRequest
            {
                Status = "Cancelled",
                InternalNotes = $"Payment {payload.TransactionId} failed - {payload.ErrorMessage} ({payload.ProviderErrorCode})",
                CustomerNotes = "Payment failed before completion."
            };

            try
            {
                _ = await _orderStatusService.CreateOrderStatusAsync(
                    payload.OrderId,
                    request,
                    "System-PaymentService",
                    context.CancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                if (await PaymentOutcomeConsumerHelper.IsDuplicateCancelledOutcomeAsync(
                        _orderStatusService,
                        payload.OrderId,
                        payload.TransactionId,
                        context.CancellationToken))
                {
                    Log.DuplicatePaymentFailedEventIgnored(_logger, payload.OrderId, payload.TransactionId);
                    return;
                }

                Log.FailedToCancelOrderAfterPaymentFailure(_logger, ex, payload.OrderId, ex.Message);
                throw;
            }
        }

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Warning, Message = "PaymentFailedEvent received without payload; skipping")]
            public static partial void PaymentFailedEventPayloadMissing(ILogger logger);

            [LoggerMessage(Level = LogLevel.Debug, Message = "Ignoring untargeted PaymentFailedEvent for order {OrderId}, transaction {TransactionId}")]
            public static partial void IgnoringUntargetedPaymentFailedEvent(ILogger logger, string orderId, Guid transactionId);

            [LoggerMessage(Level = LogLevel.Information, Message = "Ignoring duplicate PaymentFailedEvent for order {OrderId}, transaction {TransactionId}")]
            public static partial void DuplicatePaymentFailedEventIgnored(ILogger logger, string orderId, Guid transactionId);

            [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to cancel order {OrderId} after payment failure: {Message}")]
            public static partial void FailedToCancelOrderAfterPaymentFailure(ILogger logger, Exception exception, string orderId, string message);
        }
    }

    /// <summary>
    /// Consumer for payment expiration events that cancel the associated accepted order.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="PaymentExpiredEventConsumer"/> class.
    /// </remarks>
    /// <param name="orderStatusService">The order status service.</param>
    /// <param name="logger">The logger instance.</param>
    public sealed partial class PaymentExpiredEventConsumer(
        IOrderStatusService orderStatusService,
        ILogger<PaymentExpiredEventConsumer> logger) : IConsumer<PaymentExpiredEvent>
    {
        private readonly IOrderStatusService _orderStatusService = orderStatusService;
        private readonly ILogger<PaymentExpiredEventConsumer> _logger = logger;

        /// <summary>
        /// Consumes a payment expiration event and cancels the associated order when routed here.
        /// </summary>
        /// <param name="context">The message context containing the payment expiration event.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task Consume(ConsumeContext<PaymentExpiredEvent> context)
        {
            PaymentExpiredEvent message = context.Message;
            PaymentExpiredEventPayload? payload = message.Payload;

            if (payload is null)
            {
                Log.PaymentExpiredEventPayloadMissing(_logger);
                return;
            }

            if (!PaymentOutcomeConsumerHelper.IsRoutedToOrderService(message.ConsumedBy))
            {
                Log.IgnoringUntargetedPaymentExpiredEvent(_logger, payload.OrderId, payload.TransactionId);
                return;
            }

            var request = new CreateOrderStatusRequest
            {
                Status = "Cancelled",
                InternalNotes = $"Payment {payload.TransactionId} expired - {payload.Reason}",
                CustomerNotes = "Payment session expired before completion."
            };

            try
            {
                _ = await _orderStatusService.CreateOrderStatusAsync(
                    payload.OrderId,
                    request,
                    "System-PaymentService",
                    context.CancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                if (await PaymentOutcomeConsumerHelper.IsDuplicateCancelledOutcomeAsync(
                        _orderStatusService,
                        payload.OrderId,
                        payload.TransactionId,
                        context.CancellationToken))
                {
                    Log.DuplicatePaymentExpiredEventIgnored(_logger, payload.OrderId, payload.TransactionId);
                    return;
                }

                Log.FailedToCancelOrderAfterPaymentExpiration(_logger, ex, payload.OrderId, ex.Message);
                throw;
            }
        }

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Warning, Message = "PaymentExpiredEvent received without payload; skipping")]
            public static partial void PaymentExpiredEventPayloadMissing(ILogger logger);

            [LoggerMessage(Level = LogLevel.Debug, Message = "Ignoring untargeted PaymentExpiredEvent for order {OrderId}, transaction {TransactionId}")]
            public static partial void IgnoringUntargetedPaymentExpiredEvent(ILogger logger, string orderId, Guid transactionId);

            [LoggerMessage(Level = LogLevel.Information, Message = "Ignoring duplicate PaymentExpiredEvent for order {OrderId}, transaction {TransactionId}")]
            public static partial void DuplicatePaymentExpiredEventIgnored(ILogger logger, string orderId, Guid transactionId);

            [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to cancel order {OrderId} after payment expiration: {Message}")]
            public static partial void FailedToCancelOrderAfterPaymentExpiration(ILogger logger, Exception exception, string orderId, string message);
        }
    }

    internal static class PaymentOutcomeConsumerHelper
    {
        public static bool IsRoutedToOrderService(IReadOnlyList<string>? consumedBy)
        {
            return consumedBy?.Any(
                consumer => consumer.Equals("OrderService", StringComparison.OrdinalIgnoreCase)) == true;
        }

        public static async Task<bool> IsDuplicateCancelledOutcomeAsync(
            IOrderStatusService orderStatusService,
            string orderId,
            Guid transactionId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<DTOs.Response.OrderStatusResponse> history =
                await orderStatusService.GetOrderStatusHistoryAsync(orderId, cancellationToken);
            string transactionIdText = transactionId.ToString();

            return history.Any(status =>
                string.Equals(status.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) &&
                status.InternalNotes?.Contains(transactionIdText, StringComparison.OrdinalIgnoreCase) == true);
        }
    }
}
