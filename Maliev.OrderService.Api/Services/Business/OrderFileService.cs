using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Api.Mapping;
using Maliev.OrderService.Api.Services.External;
using Maliev.OrderService.Data;
using Maliev.OrderService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Maliev.OrderService.Api.Services.Business
{
    /// <summary>
    /// Service for managing order files
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="OrderFileService"/> class.
    /// </remarks>
    /// <param name="context">The database context</param>
    /// <param name="uploadService">The upload service client</param>
    /// <param name="logger">The logger instance</param>
    public partial class OrderFileService(
        OrderDbContext context,
        IUploadServiceClient uploadService,
        ILogger<OrderFileService> logger) : IOrderFileService
    {
        private readonly OrderDbContext _context = context;
        private readonly IUploadServiceClient _uploadService = uploadService;
        private readonly ILogger<OrderFileService> _logger = logger;

        /// <inheritdoc />
        public async Task<List<OrderFileResponse>> GetOrderFilesAsync(string orderId, CancellationToken cancellationToken = default)
        {
            List<OrderFile> files = await _context.OrderFiles
                .Where(f => f.OrderId == orderId && f.DeletedAt == null)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync(cancellationToken);

            return [.. files.Select(f => f.ToOrderFileResponse())];
        }

        /// <inheritdoc />
        public async Task<OrderFileResponse> UploadOrderFileAsync(
            string orderId,
            UploadOrderFileRequest request,
            Stream fileStream,
            string fileName,
            string uploadedBy,
            CancellationToken cancellationToken = default)
        {
            // Verify order exists
            Order? order = await _context.Orders.FindAsync([orderId], cancellationToken) ?? throw new InvalidOperationException($"Order {orderId} not found");

            // Upload to Upload Service
            string objectPath = $"orders/{orderId}/files/{fileName}";
            UploadFileResult uploadResult = await _uploadService.UploadFileAsync(objectPath, fileStream, "application/octet-stream", cancellationToken);

            // Create file record
            var orderFile = request.ToOrderFile();
            orderFile.OrderId = orderId;
            orderFile.FileName = fileName;
            orderFile.ObjectPath = uploadResult.ObjectPath;
            orderFile.FileSize = uploadResult.FileSizeBytes;
            orderFile.FileType = uploadResult.ContentType;
            orderFile.AccessLevel = order.IsConfidential ? "Confidential" : "Internal";
            orderFile.UploadedBy = uploadedBy;
            orderFile.UploadedAt = DateTime.UtcNow;

            _ = _context.OrderFiles.Add(orderFile);
            _ = await _context.SaveChangesAsync(cancellationToken);

            return orderFile.ToOrderFileResponse();
        }

        /// <inheritdoc />
        public async Task<(Stream? FileStream, string FileName, string ContentType)> DownloadOrderFileAsync(
            string orderId,
            long fileId,
            CancellationToken cancellationToken = default)
        {
            OrderFile? file = await _context.OrderFiles
                .FirstOrDefaultAsync(f => f.OrderId == orderId && f.FileId == fileId && f.DeletedAt == null, cancellationToken);

            if (file == null)
            {
                return (null, string.Empty, string.Empty);
            }

            Stream? stream = await _uploadService.DownloadFileAsync(file.ObjectPath, cancellationToken);
            return (stream, file.FileName, file.FileType);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteOrderFileAsync(string orderId, long fileId, CancellationToken cancellationToken = default)
        {
            OrderFile? file = await _context.OrderFiles
                .FirstOrDefaultAsync(f => f.OrderId == orderId && f.FileId == fileId && f.DeletedAt == null, cancellationToken);

            if (file == null)
            {
                return false;
            }

            // Soft delete
            file.DeletedAt = DateTime.UtcNow;
            _ = await _context.SaveChangesAsync(cancellationToken);

            // Note: Hard deletion from GCS is typically handled by a background cleanup service 
            // after the retention period (30 days) has passed.
            // Explicit immediate deletion can be awaited if necessary, but we follow the soft-delete policy.

            return true;
        }

        private static partial class Log
        {
            // LoggerMessage delegates can be added here as needed
        }
    }
}
