namespace Maliev.OrderService.Api.Services.External;

/// <summary>
/// Interface for interacting with the external Upload Service
/// </summary>
public interface IUploadServiceClient
{
    /// <summary>
    /// Uploads a file to the storage service
    /// </summary>
    /// <param name="objectPath">The path where the file should be stored</param>
    /// <param name="fileStream">The file content stream</param>
    /// <param name="contentType">The content type of the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The upload result</returns>
    Task<UploadFileResult> UploadFileAsync(string objectPath, Stream fileStream, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from the storage service
    /// </summary>
    /// <param name="objectPath">The path of the file to download</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The file stream or null if not found</returns>
    Task<Stream?> DownloadFileAsync(string objectPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the storage service
    /// </summary>
    /// <param name="objectPath">The path of the file to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> DeleteFileAsync(string objectPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a file upload operation
/// </summary>
public class UploadFileResult
{
    /// <summary>Gets or sets the object path</summary>
    public required string ObjectPath { get; set; }
    
    /// <summary>Gets or sets the file size in bytes</summary>
    public long FileSizeBytes { get; set; }
    
    /// <summary>Gets or sets the content type</summary>
    public required string ContentType { get; set; }
    
    /// <summary>Gets or sets when the file was uploaded</summary>
    public DateTime UploadedAt { get; set; }
}
