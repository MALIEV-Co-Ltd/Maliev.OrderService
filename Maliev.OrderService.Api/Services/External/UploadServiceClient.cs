using System.Net.Http.Json;

namespace Maliev.OrderService.Api.Services.External;

public partial class UploadServiceClient : IUploadServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UploadServiceClient> _logger;

    public UploadServiceClient(HttpClient httpClient, ILogger<UploadServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UploadFileResult> UploadFileAsync(string objectPath, Stream fileStream, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", objectPath);
            content.Add(new StringContent(objectPath), "objectPath");

            var response = await _httpClient.PostAsync("/api/v1/files/upload", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<UploadFileResult>(cancellationToken: cancellationToken);
            return result ?? throw new InvalidOperationException("Upload service returned null result");
        }
        catch (HttpRequestException ex)
        {
            Log.FailedToUploadFile(_logger, objectPath, ex);
            throw;
        }
    }

    public async Task<Stream?> DownloadFileAsync(string objectPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/files/download?objectPath={Uri.EscapeDataString(objectPath)}", cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Log.FailedToDownloadFile(_logger, objectPath, ex);
            return null;
        }
    }

    public async Task<bool> DeleteFileAsync(string objectPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/v1/files?objectPath={Uri.EscapeDataString(objectPath)}", cancellationToken);
            response.EnsureSuccessStatusCode();
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
