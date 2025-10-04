using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;

namespace Maliev.OrderService.Api.Services.Business;

public interface IOrderFileService
{
    Task<List<OrderFileResponse>> GetOrderFilesAsync(string orderId, CancellationToken cancellationToken = default);
    Task<OrderFileResponse> UploadOrderFileAsync(string orderId, UploadOrderFileRequest request, Stream fileStream, string fileName, string uploadedBy, CancellationToken cancellationToken = default);
    Task<(Stream? FileStream, string FileName, string ContentType)> DownloadOrderFileAsync(string orderId, long fileId, CancellationToken cancellationToken = default);
    Task<bool> DeleteOrderFileAsync(string orderId, long fileId, CancellationToken cancellationToken = default);
}
