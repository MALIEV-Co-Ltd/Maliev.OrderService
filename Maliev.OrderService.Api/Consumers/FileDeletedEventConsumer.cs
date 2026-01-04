using Maliev.MessagingContracts.Generated;
using Maliev.OrderService.Data;
using Maliev.OrderService.Data.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Maliev.OrderService.Api.Consumers
{
    /// <summary>
    /// Consumes FileDeletedEvent from UploadService to clean up local order file references.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="FileDeletedEventConsumer"/> class.
    /// </remarks>
    /// <param name="dbContext">The database context</param>
    /// <param name="logger">The logger instance</param>
    public partial class FileDeletedEventConsumer(OrderDbContext dbContext, ILogger<FileDeletedEventConsumer> logger) : IConsumer<FileDeletedEvent>
    {
        private readonly OrderDbContext _dbContext = dbContext;
        private readonly ILogger<FileDeletedEventConsumer> _logger = logger;

        /// <summary>
        /// Consumes a FileDeletedEvent and marks local order file references as deleted.
        /// </summary>
        /// <param name="context">The message context containing the FileDeletedEvent</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task Consume(ConsumeContext<FileDeletedEvent> context)
        {
            FileDeletedEvent @event = context.Message;
            FileDeletedEventPayload payload = @event.Payload;

            // Only process events for order-service files
            if (payload.ServiceId != "order-service")
            {
                return;
            }

            Log.ConsumingFileDeletedEvent(_logger, payload.FileId, payload.StoragePath);

            // Find order files referencing this storage path
            List<OrderFile> orderFiles = await _dbContext.OrderFiles
                .Where(f => f.ObjectPath == payload.StoragePath)
                .ToListAsync(context.CancellationToken);

            if (orderFiles.Count > 0)
            {
                foreach (OrderFile? file in orderFiles)
                {
                    file.DeletedAt = payload.DeletedAt.DateTime;
                }

                _ = await _dbContext.SaveChangesAsync(context.CancellationToken);

                Log.OrderFileReferencesMarkedAsDeleted(_logger, orderFiles.Count);
            }
        }

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Information, Message = "Consuming FileDeletedEvent for FileId: {FileId}, StoragePath: {StoragePath}")]
            public static partial void ConsumingFileDeletedEvent(ILogger logger, string fileId, string storagePath);

            [LoggerMessage(Level = LogLevel.Information, Message = "Marked {Count} order file references as deleted")]
            public static partial void OrderFileReferencesMarkedAsDeleted(ILogger logger, int count);
        }
    }
}
