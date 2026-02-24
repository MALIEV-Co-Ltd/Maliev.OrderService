using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;

namespace Maliev.OrderService.Api.Services.Business
{
    /// <summary>
    /// Service for managing order files
    /// </summary>
    public interface IOrderFileService
    {
        /// <summary>
        /// Gets all files associated with an order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of order files</returns>
        Task<List<OrderFileResponse>> GetOrderFilesAsync(string orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a file to an order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="request">File upload metadata</param>
        /// <param name="fileStream">The file content stream</param>
        /// <param name="fileName">The file name</param>
        /// <param name="uploadedBy">User who uploaded the file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The uploaded file details</returns>
        Task<OrderFileResponse> UploadOrderFileAsync(string orderId, UploadOrderFileRequest request, Stream fileStream, string fileName, string uploadedBy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a file
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="fileId">The file ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tuple containing file stream, name, and content type</returns>
        Task<(Stream? FileStream, string FileName, string ContentType)> DownloadOrderFileAsync(string orderId, long fileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a file (soft delete)
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="fileId">The file ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false if not found</returns>
        Task<bool> DeleteOrderFileAsync(string orderId, long fileId, CancellationToken cancellationToken = default);
    }
}
