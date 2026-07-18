using System.Net.Http.Json;
using System.Text.Json;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;

namespace Maliev.OrderService.Api.Services.External;

/// <summary>
/// Client for interacting with the Geometry Service
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GeometryServiceClient"/> class.
/// </remarks>
/// <param name="httpClient">The HTTP client</param>
/// <param name="logger">The logger instance</param>
public partial class GeometryServiceClient(HttpClient httpClient, ILogger<GeometryServiceClient> logger) : IGeometryServiceClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<GeometryServiceClient> _logger = logger;

    /// <inheritdoc />
    public async Task<QualityCheckResponse?> QualityCheckAsync(string uploadId, QualityCheckRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            Log.StartingQualityCheck(_logger, uploadId);

            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
                $"/geometry/uploads/{uploadId}/quality-check",
                request,
                cancellationToken
            );

            _ = response.EnsureSuccessStatusCode();

            QualityCheckResponse? result = await response.Content.ReadFromJsonAsync<QualityCheckResponse>(cancellationToken: cancellationToken);

            if (result == null)
            {
                Log.QualityCheckReturnedNull(_logger, uploadId);
                return null;
            }

            Log.QualityCheckCompleted(_logger, uploadId, result.Status, result.Quality.FaceCount);
            return result;
        }
        catch (HttpRequestException ex)
        {
            Log.QualityCheckFailed(_logger, uploadId, ex);
            throw;
        }
        catch (JsonException ex)
        {
            Log.QualityCheckDeserializationFailed(_logger, uploadId, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DfmAnalysisResponse?> AnalyzeForProcessAsync(string uploadId, string processCode, int timeout = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            Log.StartingProcessAnalysis(_logger, uploadId, processCode, timeout);

            HttpResponseMessage response = await _httpClient.PostAsync(
                $"/geometry/uploads/{uploadId}/dfm/{processCode}?timeout={timeout}",
                null,
                cancellationToken
            );

            _ = response.EnsureSuccessStatusCode();

            DfmAnalysisResponse? result = await response.Content.ReadFromJsonAsync<DfmAnalysisResponse>(cancellationToken: cancellationToken);

            if (result == null)
            {
                Log.ProcessAnalysisReturnedNull(_logger, uploadId, processCode);
                return null;
            }

            Log.ProcessAnalysisCompleted(_logger, uploadId, processCode, result.Status, result.DfmReport.Issues.Count);
            return result;
        }
        catch (HttpRequestException ex)
        {
            Log.ProcessAnalysisFailed(_logger, uploadId, processCode, ex);
            throw;
        }
        catch (JsonException ex)
        {
            Log.ProcessAnalysisDeserializationFailed(_logger, uploadId, processCode, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CleanupUploadAsync(string uploadId, CancellationToken cancellationToken = default)
    {
        try
        {
            Log.StartingCleanup(_logger, uploadId);

            HttpResponseMessage response = await _httpClient.DeleteAsync(
                $"/geometry/uploads/{uploadId}",
                cancellationToken
            );

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Log.CleanupNotFound(_logger, uploadId);
                return false;
            }

            _ = response.EnsureSuccessStatusCode();
            Log.CleanupCompleted(_logger, uploadId);
            return true;
        }
        catch (HttpRequestException ex)
        {
            Log.CleanupFailed(_logger, uploadId, ex);
            return false;
        }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Starting quality check for upload {UploadId}")]
        public static partial void StartingQualityCheck(ILogger logger, string uploadId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Quality check completed for upload {UploadId} with status {Status}, face count {FaceCount}")]
        public static partial void QualityCheckCompleted(ILogger logger, string uploadId, string status, int faceCount);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Quality check returned null for upload {UploadId}")]
        public static partial void QualityCheckReturnedNull(ILogger logger, string uploadId);

        [LoggerMessage(Level = LogLevel.Error, Message = "Quality check failed for upload {UploadId}")]
        public static partial void QualityCheckFailed(ILogger logger, string uploadId, Exception ex);

        [LoggerMessage(Level = LogLevel.Error, Message = "Quality check deserialization failed for upload {UploadId}")]
        public static partial void QualityCheckDeserializationFailed(ILogger logger, string uploadId, Exception ex);

        [LoggerMessage(Level = LogLevel.Information, Message = "Starting process analysis for upload {UploadId}, process {ProcessCode}, timeout {Timeout}s")]
        public static partial void StartingProcessAnalysis(ILogger logger, string uploadId, string processCode, int timeout);

        [LoggerMessage(Level = LogLevel.Information, Message = "Process analysis completed for upload {UploadId}, process {ProcessCode}, status {Status}, issue count {IssueCount}")]
        public static partial void ProcessAnalysisCompleted(ILogger logger, string uploadId, string processCode, string status, int issueCount);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Process analysis returned null for upload {UploadId}, process {ProcessCode}")]
        public static partial void ProcessAnalysisReturnedNull(ILogger logger, string uploadId, string processCode);

        [LoggerMessage(Level = LogLevel.Error, Message = "Process analysis failed for upload {UploadId}, process {ProcessCode}")]
        public static partial void ProcessAnalysisFailed(ILogger logger, string uploadId, string processCode, Exception ex);

        [LoggerMessage(Level = LogLevel.Error, Message = "Process analysis deserialization failed for upload {UploadId}, process {ProcessCode}")]
        public static partial void ProcessAnalysisDeserializationFailed(ILogger logger, string uploadId, string processCode, Exception ex);

        [LoggerMessage(Level = LogLevel.Information, Message = "Starting cleanup for upload {UploadId}")]
        public static partial void StartingCleanup(ILogger logger, string uploadId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Cleanup completed for upload {UploadId}")]
        public static partial void CleanupCompleted(ILogger logger, string uploadId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Cleanup not found for upload {UploadId}")]
        public static partial void CleanupNotFound(ILogger logger, string uploadId);

        [LoggerMessage(Level = LogLevel.Error, Message = "Cleanup failed for upload {UploadId}")]
        public static partial void CleanupFailed(ILogger logger, string uploadId, Exception ex);
    }
}
