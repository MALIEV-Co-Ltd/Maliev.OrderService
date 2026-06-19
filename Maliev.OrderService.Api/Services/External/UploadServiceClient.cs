namespace Maliev.OrderService.Api.Services.External
{
    /// <summary>
    /// Client for interacting with the external Upload Service
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="UploadServiceClient"/> class.
    /// </remarks>
    /// <param name="httpClient">The HTTP client</param>
    /// <param name="logger">The logger instance</param>
    public partial class UploadServiceClient(HttpClient httpClient, ILogger<UploadServiceClient> logger) : IUploadServiceClient
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<UploadServiceClient> _logger = logger;

        /// <inheritdoc />
        public async Task<UploadFileResult> UploadFileAsync(string objectPath, Stream fileStream, string contentType, CancellationToken cancellationToken = default)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream, cancellationToken);
                var artifactRequest = new UploadArtifactRequest(
                    ArtifactId: Guid.NewGuid(),
                    ParentUploadId: Guid.NewGuid(),
                    StoragePath: objectPath,
                    ContentType: contentType,
                    ArtifactData: Convert.ToBase64String(memoryStream.ToArray()));

                HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
                    "/upload/v1/uploads/artifacts",
                    artifactRequest,
                    cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new InvalidOperationException(
                        $"UploadService artifact upload failed with HTTP {(int)response.StatusCode}: {body}");
                }

                ArtifactUploadResponse? result = await response.Content.ReadFromJsonAsync<ArtifactUploadResponse>(cancellationToken: cancellationToken);
                if (result == null)
                {
                    throw new InvalidOperationException("Upload service returned null result");
                }

                return new UploadFileResult
                {
                    ObjectPath = result.StoragePath,
                    FileSizeBytes = memoryStream.Length,
                    ContentType = contentType,
                    UploadedAt = DateTime.UtcNow
                };
            }
            catch (HttpRequestException ex)
            {
                Log.FailedToUploadFile(_logger, objectPath, ex);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<Stream?> DownloadFileAsync(string objectPath, CancellationToken cancellationToken = default)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"/api/v1/files/download?objectPath={Uri.EscapeDataString(objectPath)}", cancellationToken);
                _ = response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStreamAsync(cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                Log.FailedToDownloadFile(_logger, objectPath, ex);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteFileAsync(string objectPath, CancellationToken cancellationToken = default)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.DeleteAsync($"/api/v1/files?objectPath={Uri.EscapeDataString(objectPath)}", cancellationToken);
                _ = response.EnsureSuccessStatusCode();
                return true;
            }
            catch (HttpRequestException ex)
            {
                Log.FailedToDeleteFile(_logger, objectPath, ex);
                return false;
            }
        }

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Error, Message = "Failed to upload file to {ObjectPath}")]
            public static partial void FailedToUploadFile(ILogger logger, string objectPath, Exception ex);

            [LoggerMessage(Level = LogLevel.Error, Message = "Failed to download file from {ObjectPath}")]
            public static partial void FailedToDownloadFile(ILogger logger, string objectPath, Exception ex);

            [LoggerMessage(Level = LogLevel.Error, Message = "Failed to delete file at {ObjectPath}")]
            public static partial void FailedToDeleteFile(ILogger logger, string objectPath, Exception ex);
        }

        private sealed record UploadArtifactRequest(
            Guid ArtifactId,
            Guid ParentUploadId,
            string StoragePath,
            string ContentType,
            string ArtifactData);

        private sealed record ArtifactUploadResponse(
            Guid ArtifactId,
            string StoragePath,
            string DownloadUrl);
    }
}
