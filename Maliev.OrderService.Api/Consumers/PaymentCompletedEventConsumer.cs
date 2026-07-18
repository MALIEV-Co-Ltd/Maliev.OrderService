using Maliev.MessagingContracts.Contracts.Payments;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.Services.Business;
using MassTransit;

namespace Maliev.OrderService.Api.Consumers
{
    /// <summary>
    /// Consumer for PaymentCompletedEvent - Updates order status to "Paid" when payment is completed
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="PaymentCompletedEventConsumer"/> class.
    /// </remarks>
    /// <param name="orderStatusService">The order status service</param>
    /// <param name="logger">The logger instance</param>
    public partial class PaymentCompletedEventConsumer(
        IOrderStatusService orderStatusService,
        ILogger<PaymentCompletedEventConsumer> logger) : IConsumer<PaymentCompletedEvent>
    {
        private readonly IOrderStatusService _orderStatusService = orderStatusService;
        private readonly ILogger<PaymentCompletedEventConsumer> _logger = logger;

        /// <summary>
        /// Consumes a PaymentCompletedEvent and updates the corresponding order to "Paid" status
        /// </summary>
        /// <param name="context">The message context containing the PaymentCompletedEvent</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
        {
            PaymentCompletedEvent @event = context.Message;
            PaymentCompletedEventPayload? payload = @event.Payload;

            if (payload is null)
            {
                Log.PaymentCompletedEventPayloadMissing(_logger);
                return;
            }

            if (!IsRoutedToOrderService(@event))
            {
                Log.IgnoringUntargetedPaymentCompletedEvent(_logger, payload.OrderId, payload.PaymentId);
                return;
            }

            Log.ConsumingPaymentCompletedEvent(_logger, payload.OrderId, payload.PaymentId);

            try
            {
                // Update order status to "Paid"
                string orderIdentifier = ResolveOrderIdentifier(payload);
                var statusRequest = new CreateOrderStatusRequest
                {
                    Status = "Paid",
                    InternalNotes = $"Payment {payload.PaymentId} completed - Amount: {payload.Amount} {payload.Currency}",
                    PaymentId = payload.PaymentId.ToString(),
                    PaidAmount = Convert.ToDecimal(payload.Amount),
                    PaymentCurrency = payload.Currency,
                    PaymentProviderName = payload.ProviderName
                };

                _ = await _orderStatusService.CreateOrderStatusAsync(
                    orderIdentifier,
                    statusRequest,
                    updatedBy: "System-PaymentService",
                    context.CancellationToken);

                Log.OrderUpdatedToPaidStatus(_logger, payload.OrderId);
            }
            catch (InvalidOperationException ex)
            {
                if (await IsDuplicatePaidEventAsync(payload, ResolveOrderIdentifier(payload), context.CancellationToken))
                {
                    Log.DuplicatePaymentCompletedEventIgnored(_logger, payload.OrderId, payload.PaymentId);
                    return;
                }

                // Order not found or invalid state transition
                Log.FailedToUpdateOrderToPaidStatus(_logger, ex, payload.OrderId, ex.Message);

                // Re-throw to trigger retry/dead-letter.
                // Silently ignoring a payment completion event is a data integrity risk.
                throw;
            }
            catch (Exception ex)
            {
                Log.UnexpectedErrorProcessingPaymentCompletedEvent(_logger, ex, payload.OrderId);
                throw; // Rethrow to trigger retry/dead-letter
            }
        }

        private async Task<bool> IsDuplicatePaidEventAsync(
            PaymentCompletedEventPayload payload,
            string orderIdentifier,
            CancellationToken cancellationToken)
        {
            List<DTOs.Response.OrderStatusResponse> history;
            try
            {
                history = await _orderStatusService.GetOrderStatusHistoryAsync(orderIdentifier, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.PaymentStatusHistoryLookupFailed(_logger, ex, payload.OrderId, payload.PaymentId);
                return false;
            }

            string paymentId = payload.PaymentId.ToString();
            return history?.Any(status =>
                string.Equals(status.Status, "Paid", StringComparison.OrdinalIgnoreCase) &&
                status.InternalNotes?.Contains(paymentId, StringComparison.OrdinalIgnoreCase) == true) == true;
        }

        private static string ResolveOrderIdentifier(PaymentCompletedEventPayload payload)
        {
            return string.IsNullOrWhiteSpace(payload.OrderNumber)
                ? payload.OrderId.ToString("D")
                : payload.OrderNumber;
        }

        private static bool IsRoutedToOrderService(PaymentCompletedEvent message)
        {
            return message.ConsumedBy?.Any(
                consumer => consumer.Equals("OrderService", StringComparison.OrdinalIgnoreCase)) == true;
        }

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Warning, Message = "PaymentCompletedEvent received without payload; skipping")]
            public static partial void PaymentCompletedEventPayloadMissing(ILogger logger);

            [LoggerMessage(Level = LogLevel.Debug, Message = "Ignoring untargeted PaymentCompletedEvent for order {OrderId}, payment {PaymentId}")]
            public static partial void IgnoringUntargetedPaymentCompletedEvent(ILogger logger, Guid orderId, Guid paymentId);

            [LoggerMessage(Level = LogLevel.Information, Message = "Consuming PaymentCompletedEvent for order {OrderId}, payment {PaymentId}")]
            public static partial void ConsumingPaymentCompletedEvent(ILogger logger, Guid orderId, Guid paymentId);

            [LoggerMessage(Level = LogLevel.Information, Message = "Successfully updated order {OrderId} to Paid status after payment completion")]
            public static partial void OrderUpdatedToPaidStatus(ILogger logger, Guid orderId);

            [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to update order {OrderId} to Paid status: {Message}")]
            public static partial void FailedToUpdateOrderToPaidStatus(ILogger logger, Exception exception, Guid orderId, string message);

            [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error processing PaymentCompletedEvent for order {OrderId}")]
            public static partial void UnexpectedErrorProcessingPaymentCompletedEvent(ILogger logger, Exception exception, Guid orderId);

            [LoggerMessage(Level = LogLevel.Information, Message = "Ignoring duplicate PaymentCompletedEvent for order {OrderId}, payment {PaymentId}")]
            public static partial void DuplicatePaymentCompletedEventIgnored(ILogger logger, Guid orderId, Guid paymentId);

            [LoggerMessage(Level = LogLevel.Warning, Message = "Could not inspect status history while checking duplicate PaymentCompletedEvent for order {OrderId}, payment {PaymentId}")]
            public static partial void PaymentStatusHistoryLookupFailed(ILogger logger, Exception exception, Guid orderId, Guid paymentId);
        }
    }
}
