using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Geometry;
using Maliev.OrderService.Infrastructure.Persistence;
using Maliev.OrderService.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Maliev.OrderService.Api.Consumers;

/// <summary>
/// Consumes PreviewImagesGeneratedEvent from GeometryService to store preview image metadata.
/// </summary>
public partial class PreviewImagesGeneratedEventConsumer(OrderDbContext dbContext, ILogger<PreviewImagesGeneratedEventConsumer> logger) : IConsumer<PreviewImagesGeneratedEvent>
{
    private readonly OrderDbContext _dbContext = dbContext;
    private readonly ILogger<PreviewImagesGeneratedEventConsumer> _logger = logger;

    /// <summary>
    /// Consumes a PreviewImagesGeneratedEvent and stores the preview image metadata.
    /// </summary>
    public async Task Consume(ConsumeContext<PreviewImagesGeneratedEvent> context)
    {
        PreviewImagesGeneratedEvent @event = context.Message;
        PreviewImagesGeneratedEventPayload payload = @event.Payload;

        Log.ConsumingPreviewImagesEvent(_logger, payload.StoragePath);

        OrderFile? orderFile = await _dbContext.OrderFiles
            .FirstOrDefaultAsync(f => f.ObjectPath == payload.StoragePath, context.CancellationToken);

        if (orderFile == null)
        {
            Log.OrderFileNotFound(_logger, payload.StoragePath);
            return;
        }

        if (!orderFile.IsPrimary)
        {
            Log.NotPrimaryFile(_logger, orderFile.FileId, orderFile.ObjectPath);
            return;
        }

        string orderId = orderFile.OrderId;
        var previewImages = payload.PreviewImages;

        var sides = new (string Side, string? Path)[]
        {
            ("Front", previewImages.FrontSmall),
            ("Left", previewImages.LeftSmall),
            ("Right", previewImages.RightSmall),
            ("Back", previewImages.BackSmall),
            ("Top", previewImages.TopSmall),
            ("Bottom", previewImages.BottomSmall)
        };

        List<OrderPreviewImage> existingPreviews = await _dbContext.OrderPreviewImages
            .Where(p => p.OrderId == orderId)
            .ToListAsync(context.CancellationToken);

        if (existingPreviews.Count > 0)
        {
            _dbContext.OrderPreviewImages.RemoveRange(existingPreviews);
            Log.RemovedExistingPreviews(_logger, existingPreviews.Count, orderId);
        }

        foreach (var (side, path) in sides)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var previewImage = new OrderPreviewImage
                {
                    OrderId = orderId,
                    Side = side,
                    StoragePath = path,
                    GeneratedAt = payload.GeneratedAt.DateTime,
                    SourceFileId = orderFile.FileId
                };
                _dbContext.OrderPreviewImages.Add(previewImage);
            }
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);

        int storedCount = sides.Count(s => !string.IsNullOrEmpty(s.Path));
        Log.PreviewImagesStored(_logger, orderId, storedCount);
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Consuming PreviewImagesGeneratedEvent for storage path: {StoragePath}")]
        public static partial void ConsumingPreviewImagesEvent(ILogger logger, string storagePath);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Order file not found for storage path: {StoragePath}")]
        public static partial void OrderFileNotFound(ILogger logger, string storagePath);

        [LoggerMessage(Level = LogLevel.Information, Message = "File {FileId} ({StoragePath}) is not marked as primary, skipping preview images")]
        public static partial void NotPrimaryFile(ILogger logger, long fileId, string storagePath);

        [LoggerMessage(Level = LogLevel.Information, Message = "Removed {Count} existing preview images for order {OrderId}")]
        public static partial void RemovedExistingPreviews(ILogger logger, int count, string orderId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Stored {Count} preview images for order {OrderId}")]
        public static partial void PreviewImagesStored(ILogger logger, string orderId, int count);
    }
}
