using Maliev.OrderService.Api.DTOs.Response;

namespace Maliev.OrderService.Api.Services.Business;

/// <summary>
/// Service for managing order preview images
/// </summary>
public interface IOrderPreviewImageService
{
    /// <summary>
    /// Gets all preview images for an order
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of preview images</returns>
    Task<List<OrderPreviewImageResponse>> GetOrderPreviewImagesAsync(string orderId, CancellationToken cancellationToken = default);
}
