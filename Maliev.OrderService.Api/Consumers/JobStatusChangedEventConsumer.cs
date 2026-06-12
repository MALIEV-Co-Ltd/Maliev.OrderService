using Maliev.MessagingContracts.Contracts.Jobs;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Api.Services.Business;
using MassTransit;

namespace Maliev.OrderService.Api.Consumers
{
    /// <summary>
    /// Consumes final job completion events and advances the parent order to finished.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="JobStatusChangedEventConsumer"/> class.
    /// </remarks>
    /// <param name="orderStatusService">The order status service.</param>
    /// <param name="logger">The logger instance.</param>
    public partial class JobStatusChangedEventConsumer(
        IOrderStatusService orderStatusService,
        ILogger<JobStatusChangedEventConsumer> logger) : IConsumer<JobStatusChangedEvent>
    {
        private readonly IOrderStatusService _orderStatusService = orderStatusService;
        private readonly ILogger<JobStatusChangedEventConsumer> _logger = logger;

        /// <summary>
        /// Consumes a job status change and updates order status when all production jobs are complete.
        /// </summary>
        /// <param name="context">The message context containing the job status change.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task Consume(ConsumeContext<JobStatusChangedEvent> context)
        {
            JobStatusChangedEventPayload payload = context.Message.Payload;
            if (!string.Equals(payload.NewStatus, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                Log.IgnoringNonCompletedJobStatus(_logger, payload.JobId, payload.NewStatus);
                return;
            }

            if (string.IsNullOrWhiteSpace(payload.OrderNumber))
            {
                Log.MissingOrderNumber(_logger, payload.JobId, payload.OrderId);
                throw new InvalidOperationException($"Job status event {payload.JobId} is missing orderNumber.");
            }

            try
            {
                _ = await _orderStatusService.CreateOrderStatusAsync(
                    payload.OrderNumber,
                    new CreateOrderStatusRequest
                    {
                        Status = "Finished",
                        InternalNotes = $"All production jobs completed after job {payload.JobId} completed.",
                        CustomerNotes = "Production completed; order is ready for quality review."
                    },
                    updatedBy: "System-JobService",
                    context.CancellationToken);

                Log.OrderMarkedFinished(_logger, payload.OrderNumber, payload.JobId);
            }
            catch (InvalidOperationException ex)
            {
                if (await IsCompletionAlreadyRecordedAsync(payload.OrderNumber, context.CancellationToken))
                {
                    Log.DuplicateCompletionIgnored(_logger, payload.OrderNumber, payload.JobId);
                    return;
                }

                Log.FailedToMarkOrderFinished(_logger, ex, payload.OrderNumber, payload.JobId, ex.Message);
                throw;
            }
        }

        private async Task<bool> IsCompletionAlreadyRecordedAsync(string orderNumber, CancellationToken cancellationToken)
        {
            List<OrderStatusResponse> history = await _orderStatusService.GetOrderStatusHistoryAsync(orderNumber, cancellationToken);
            return history.Any(status =>
                string.Equals(status.Status, "Finished", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status.Status, "QualityReleased", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status.Status, "Shipped", StringComparison.OrdinalIgnoreCase));
        }

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Debug, Message = "Ignoring JobStatusChangedEvent for job {JobId} with non-completed status {Status}")]
            public static partial void IgnoringNonCompletedJobStatus(ILogger logger, Guid jobId, string status);

            [LoggerMessage(Level = LogLevel.Warning, Message = "JobStatusChangedEvent for job {JobId}, order {OrderId} is missing orderNumber")]
            public static partial void MissingOrderNumber(ILogger logger, Guid jobId, Guid orderId);

            [LoggerMessage(Level = LogLevel.Information, Message = "Marked order {OrderNumber} finished after job {JobId} completed")]
            public static partial void OrderMarkedFinished(ILogger logger, string orderNumber, Guid jobId);

            [LoggerMessage(Level = LogLevel.Information, Message = "Ignoring duplicate job completion for order {OrderNumber}, job {JobId}")]
            public static partial void DuplicateCompletionIgnored(ILogger logger, string orderNumber, Guid jobId);

            [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to mark order {OrderNumber} finished after job {JobId}: {Message}")]
            public static partial void FailedToMarkOrderFinished(ILogger logger, Exception exception, string orderNumber, Guid jobId, string message);
        }
    }
}
