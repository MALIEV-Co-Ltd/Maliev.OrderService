using Maliev.MessagingContracts;
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
            PaymentCompletedEventPayload payload = @event.Payload;

            Log.ConsumingPaymentCompletedEvent(_logger, payload.OrderId, payload.PaymentId);

            try
            {
                // Update order status to "Paid"
                var statusRequest = new CreateOrderStatusRequest
                {
                    Status = "Paid",
                    InternalNotes = $"Payment {payload.PaymentId} completed - Amount: {payload.Amount} {payload.Currency}"
                };

                _ = await _orderStatusService.CreateOrderStatusAsync(
                    payload.OrderNumber,
                    statusRequest,
                    updatedBy: "System-PaymentService",
                    context.CancellationToken);

                Log.OrderUpdatedToPaidStatus(_logger, payload.OrderId);
            }
            catch (InvalidOperationException ex)
            {
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

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Information, Message = "Consuming PaymentCompletedEvent for order {OrderId}, payment {PaymentId}")]
            public static partial void ConsumingPaymentCompletedEvent(ILogger logger, Guid orderId, Guid paymentId);

            [LoggerMessage(Level = LogLevel.Information, Message = "Successfully updated order {OrderId} to Paid status after payment completion")]
            public static partial void OrderUpdatedToPaidStatus(ILogger logger, Guid orderId);

            [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to update order {OrderId} to Paid status: {Message}")]
            public static partial void FailedToUpdateOrderToPaidStatus(ILogger logger, Exception exception, Guid orderId, string message);

            [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error processing PaymentCompletedEvent for order {OrderId}")]
            public static partial void UnexpectedErrorProcessingPaymentCompletedEvent(ILogger logger, Exception exception, Guid orderId);
        }
    }
}
