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
                if (!fileStream.CanSeek)
                {
                    throw new InvalidOperationException("Direct GCS uploads require a seekable stream.");
                }

                var initiateRequest = new InitiateResumableUploadRequest(
                    Path: objectPath,
                    FileName: Path.GetFileName(objectPath),
                    ServiceName: "OrderService",
                    ContentType: contentType,
                    TotalSize: fileStream.Length,
                    Overwrite: true);

                HttpResponseMessage initiateResponse = await _httpClient.PostAsJsonAsync(
                    "/upload/v1/uploads/resumable",
                    initiateRequest,
                    cancellationToken);
                _ = initiateResponse.EnsureSuccessStatusCode();

                InitiateResumableUploadResponse session = await initiateResponse.Content
                    .ReadFromJsonAsync<InitiateResumableUploadResponse>(cancellationToken: cancellationToken)
                    ?? throw new InvalidOperationException("Upload service returned null resumable session");

                fileStream.Position = 0;
                using var gcsClient = new HttpClient();
                using var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                streamContent.Headers.ContentLength = fileStream.Length;
                streamContent.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(0, fileStream.Length - 1, fileStream.Length);

                HttpResponseMessage gcsResponse = await gcsClient.PutAsync(session.SessionUri, streamContent, cancellationToken);
                _ = gcsResponse.EnsureSuccessStatusCode();

                HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
                    $"/upload/v1/uploads/resumable/{session.UploadId}/complete",
                    new { },
                    cancellationToken);
                _ = response.EnsureSuccessStatusCode();

                UploadServiceResponse? result = await response.Content.ReadFromJsonAsync<UploadServiceResponse>(cancellationToken: cancellationToken);
                if (result == null)
                {
                    throw new InvalidOperationException("Upload service returned null result");
                }

                return new UploadFileResult
                {
                    ObjectPath = result.StoragePath,
                    FileSizeBytes = result.FileSize,
                    ContentType = result.ContentType,
                    UploadedAt = result.UploadedAt
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

        private sealed record InitiateResumableUploadRequest(
            string Path,
            string FileName,
            string ServiceName,
            string ContentType,
            long TotalSize,
            bool Overwrite);

        private sealed record InitiateResumableUploadResponse(
            string UploadId,
            string SessionUri,
            DateTime ExpiresAt,
            long TotalSize);

        private sealed record UploadServiceResponse(
            string StoragePath,
            long FileSize,
            string ContentType,
            DateTime UploadedAt);
    }
}
