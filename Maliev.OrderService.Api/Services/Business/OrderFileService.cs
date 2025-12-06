using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Api.Mapping;
using Maliev.OrderService.Api.Services.External;
using Maliev.OrderService.Data;
using Maliev.OrderService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Maliev.OrderService.Api.Services.Business;

/// <summary>
/// Service for managing order files
/// </summary>
public partial class OrderFileService : IOrderFileService
{
    private readonly OrderDbContext _context;
    private readonly IUploadServiceClient _uploadService;
    private readonly ILogger<OrderFileService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderFileService"/> class.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="uploadService">The upload service client</param>
    /// <param name="logger">The logger instance</param>
    public OrderFileService(
        OrderDbContext context,
        IUploadServiceClient uploadService,
        ILogger<OrderFileService> logger)
    {
        _context = context;
        _uploadService = uploadService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<OrderFileResponse>> GetOrderFilesAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var files = await _context.OrderFiles
            .Where(f => f.OrderId == orderId && f.DeletedAt == null)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync(cancellationToken);

        return files.Select(f => f.ToOrderFileResponse()).ToList();
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
        var order = await _context.Orders.FindAsync(new object[] { orderId }, cancellationToken);
        if (order == null)
        {
            throw new InvalidOperationException($"Order {orderId} not found");
        }

        // Upload to Upload Service
        var objectPath = $"orders/{orderId}/files/{fileName}";
        var uploadResult = await _uploadService.UploadFileAsync(objectPath, fileStream, "application/octet-stream", cancellationToken);

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

        _context.OrderFiles.Add(orderFile);
        await _context.SaveChangesAsync(cancellationToken);

        return orderFile.ToOrderFileResponse();
    }

    /// <inheritdoc />
    public async Task<(Stream? FileStream, string FileName, string ContentType)> DownloadOrderFileAsync(
        string orderId,
        long fileId,
        CancellationToken cancellationToken = default)
    {
        var file = await _context.OrderFiles
            .FirstOrDefaultAsync(f => f.OrderId == orderId && f.FileId == fileId && f.DeletedAt == null, cancellationToken);

        if (file == null)
        {
            return (null, string.Empty, string.Empty);
        }

        var stream = await _uploadService.DownloadFileAsync(file.ObjectPath, cancellationToken);
        return (stream, file.FileName, file.FileType);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteOrderFileAsync(string orderId, long fileId, CancellationToken cancellationToken = default)
    {
        var file = await _context.OrderFiles
            .FirstOrDefaultAsync(f => f.OrderId == orderId && f.FileId == fileId && f.DeletedAt == null, cancellationToken);

        if (file == null)
        {
            return false;
        }

        // Soft delete
        file.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        // Optionally delete from Upload Service (async, fire-and-forget)
        _ = Task.Run(async () => await _uploadService.DeleteFileAsync(file.ObjectPath, cancellationToken), cancellationToken);

        return true;
    }

    private static partial class Log
    {
        // LoggerMessage delegates can be added here as needed
    }
}
