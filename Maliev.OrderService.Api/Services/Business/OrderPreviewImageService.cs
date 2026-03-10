using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Domain.Entities;
using Maliev.OrderService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maliev.OrderService.Api.Services.Business;

/// <summary>
/// Service for managing order preview images
/// </summary>
public class OrderPreviewImageService(OrderDbContext dbContext) : IOrderPreviewImageService
{
    private readonly OrderDbContext _dbContext = dbContext;

    /// <summary>
    /// Gets all preview images for an order
    /// </summary>
    public async Task<List<OrderPreviewImageResponse>> GetOrderPreviewImagesAsync(string orderId, CancellationToken cancellationToken = default)
    {
        List<OrderPreviewImage> previewImages = await _dbContext.OrderPreviewImages
            .Where(p => p.OrderId == orderId)
            .OrderBy(p => p.Side)
            .ToListAsync(cancellationToken);

        return previewImages.Select(p => new OrderPreviewImageResponse
        {
            PreviewImageId = p.PreviewImageId,
            Side = p.Side,
            StoragePath = p.StoragePath,
            GeneratedAt = p.GeneratedAt
        }).ToList();
    }
}
