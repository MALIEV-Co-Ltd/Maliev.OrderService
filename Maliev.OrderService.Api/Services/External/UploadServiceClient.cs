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
                using var content = new MultipartFormDataContent();
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                content.Add(streamContent, "file", objectPath);
                content.Add(new StringContent(objectPath), "objectPath");

                HttpResponseMessage response = await _httpClient.PostAsync("/api/v1/files/upload", content, cancellationToken);
                _ = response.EnsureSuccessStatusCode();

                UploadFileResult? result = await response.Content.ReadFromJsonAsync<UploadFileResult>(cancellationToken: cancellationToken);
                return result ?? throw new InvalidOperationException("Upload service returned null result");
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
    }
}
