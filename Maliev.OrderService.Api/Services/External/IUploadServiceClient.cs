namespace Maliev.OrderService.Api.Services.External;

public interface IUploadServiceClient
{
    Task<UploadFileResult> UploadFileAsync(string objectPath, Stream fileStream, string contentType, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadFileAsync(string objectPath, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string objectPath, CancellationToken cancellationToken = default);
}

public class UploadFileResult
{
    public required string ObjectPath { get; set; }
    public long FileSizeBytes { get; set; }
    public required string ContentType { get; set; }
    public DateTime UploadedAt { get; set; }
}
