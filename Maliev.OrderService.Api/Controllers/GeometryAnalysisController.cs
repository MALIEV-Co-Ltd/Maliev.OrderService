using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Aspire.ServiceDefaults;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Api.Services.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.Controllers;

/// <summary>
/// Controller for two-phase DFM analysis via Geometry Service
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GeometryAnalysisController"/> class
/// </remarks>
[ApiController]
[ApiVersion("1")]
[Route("geometryanalysis/v{version:apiVersion}/[controller]")]
[EnableRateLimiting(RateLimitPolicies.Api)]
public partial class GeometryAnalysisController(
    IGeometryServiceClient geometryServiceClient,
    ILogger<GeometryAnalysisController> logger) : ControllerBase
{
    private readonly IGeometryServiceClient _geometryServiceClient = geometryServiceClient;
    private readonly ILogger<GeometryAnalysisController> _logger = logger;

    /// <summary>
    /// Phase 1: Perform quality check on uploaded file
    /// </summary>
    /// <param name="uploadId">Unique identifier for the upload</param>
    /// <param name="request">Quality check request with file data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quality check response with metrics</returns>
    /// <response code="200">Quality check completed successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{uploadId}/quality-check")]
    [ProducesResponseType(typeof(QualityCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QualityCheckResponse>> QualityCheck(
        [Required] string uploadId,
        [FromBody] QualityCheckRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Log.ReceivedQualityCheckRequest(_logger, uploadId);

            // Validate request
            if (string.IsNullOrWhiteSpace(request.StlBytes))
            {
                Log.MissingStlBytes(_logger, uploadId);
                return BadRequest(new ErrorMessageResponse
                {
                    Message = "Missing required field: stlBytes"
                });
            }

            // Call Geometry Service
            QualityCheckResponse? result = await _geometryServiceClient.QualityCheckAsync(uploadId, request, cancellationToken);

            if (result == null)
            {
                Log.QualityCheckReturnedNull(_logger, uploadId);
                return StatusCode(500, new ErrorMessageResponse
                {
                    Message = "Geometry service returned null response"
                });
            }

            // Check for errors from Geometry Service
            if (result.Status == "error")
            {
                Log.QualityCheckFailed(_logger, uploadId, result.ErrorType ?? "Unknown", result.Message ?? "Unknown error");
                return StatusCode(500, new ErrorMessageResponse
                {
                    Message = $"{result.ErrorType}: {result.Message}",
                });
            }

            Log.QualityCheckSuccessful(_logger, uploadId, result.Quality.FaceCount, result.Quality.Complexity);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            Log.QualityCheckHttpRequestError(_logger, uploadId, ex);
            return StatusCode(500, new ErrorMessageResponse
            {
                Message = $"Failed to communicate with Geometry Service: {ex.Message}"
            });
        }
        catch (Exception ex)
        {
            Log.QualityCheckUnexpectedError(_logger, uploadId, ex);
            return StatusCode(500, new ErrorMessageResponse
            {
                Message = $"An unexpected error occurred: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Phase 2: Analyze file for a specific manufacturing process
    /// </summary>
    /// <param name="uploadId">Unique identifier for the upload</param>
    /// <param name="processCode">Manufacturing process code (e.g., "FDM", "SLA", "CNC_MILL", "CNC_TURN")</param>
    /// <param name="timeout">Timeout in seconds (default: 30, max: 120)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>DFM analysis response with process-specific issues</returns>
    /// <response code="200">Analysis completed successfully</response>
    /// <response code="404">Upload not found (quality check not run first)</response>
    /// <response code="504">Analysis timed out</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{uploadId}/dfm/{processCode}")]
    [ProducesResponseType(typeof(DfmAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status504GatewayTimeout)]
    [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DfmAnalysisResponse>> AnalyzeForProcess(
        [Required] string uploadId,
        [Required] string processCode,
        [FromQuery] int timeout = 30,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Log.ReceivedProcessAnalysisRequest(_logger, uploadId, processCode, timeout);

            // Validate timeout
            timeout = Math.Max(5, Math.Min(120, timeout)); // Clamp between 5 and 120 seconds

            // Call Geometry Service
            DfmAnalysisResponse? result = await _geometryServiceClient.AnalyzeForProcessAsync(
                uploadId,
                processCode,
                timeout,
                cancellationToken
            );

            if (result == null)
            {
                Log.ProcessAnalysisReturnedNull(_logger, uploadId, processCode);
                return StatusCode(500, new ErrorMessageResponse
                {
                    Message = "Geometry service returned null response"
                });
            }

            // Check for timeout
            if (result.Status == "timeout")
            {
                Log.ProcessAnalysisTimedOut(_logger, uploadId, processCode, timeout);
                return StatusCode(504, new ErrorMessageResponse
                {
                    Message = $"Analysis timed out after {timeout} seconds. Try a simpler file or different process."
                });
            }

            // Check for errors from Geometry Service
            if (result.Status == "error")
            {
                if (result.ErrorType == "NotFound")
                {
                    Log.ProcessAnalysisUploadNotFound(_logger, uploadId, processCode);
                    return NotFound(new ErrorMessageResponse
                    {
                        Message = result.Message ?? "Upload not found. Please run quality check first."
                    });
                }

                Log.ProcessAnalysisFailed(_logger, uploadId, processCode, result.ErrorType ?? "Unknown", result.Message ?? "Unknown error");
                return StatusCode(500, new ErrorMessageResponse
                {
                    Message = $"{result.ErrorType}: {result.Message}",
                });
            }

            Log.ProcessAnalysisSuccessful(_logger, uploadId, processCode, result.DfmReport.Issues.Count, result.DfmReport.AnalysisTimeSeconds);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            Log.ProcessAnalysisHttpRequestError(_logger, uploadId, processCode, ex);
            return StatusCode(500, new ErrorMessageResponse
            {
                Message = $"Failed to communicate with Geometry Service: {ex.Message}"
            });
        }
        catch (Exception ex)
        {
            Log.ProcessAnalysisUnexpectedError(_logger, uploadId, processCode, ex);
            return StatusCode(500, new ErrorMessageResponse
            {
                Message = $"An unexpected error occurred: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Clean up cached file data for an upload
    /// </summary>
    /// <param name="uploadId">Unique identifier for the upload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Cleanup successful</response>
    /// <response code="404">Upload not found</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{uploadId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CleanupUpload(
        [Required] string uploadId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            Log.ReceivedCleanupRequest(_logger, uploadId);

            // Call Geometry Service
            bool success = await _geometryServiceClient.CleanupUploadAsync(uploadId, cancellationToken);

            if (!success)
            {
                Log.CleanupUploadNotFound(_logger, uploadId);
                return NotFound(new ErrorMessageResponse
                {
                    Message = $"Upload {uploadId} not found or already cleaned up"
                });
            }

            Log.CleanupSuccessful(_logger, uploadId);
            return NoContent();
        }
        catch (HttpRequestException ex)
        {
            Log.CleanupHttpRequestError(_logger, uploadId, ex);
            return StatusCode(500, new ErrorMessageResponse
            {
                Message = $"Failed to communicate with Geometry Service: {ex.Message}"
            });
        }
        catch (Exception ex)
        {
            Log.CleanupUnexpectedError(_logger, uploadId, ex);
            return StatusCode(500, new ErrorMessageResponse
            {
                Message = $"An unexpected error occurred: {ex.Message}"
            });
        }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Received quality check request for upload {UploadId}")]
        public static partial void ReceivedQualityCheckRequest(ILogger logger, string uploadId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Quality check request missing STL bytes for upload {UploadId}")]
        public static partial void MissingStlBytes(ILogger logger, string uploadId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Quality check returned null for upload {UploadId}")]
        public static partial void QualityCheckReturnedNull(ILogger logger, string uploadId);

        [LoggerMessage(Level = LogLevel.Error, Message = "Quality check failed for upload {UploadId}: {ErrorType} - {Message}")]
        public static partial void QualityCheckFailed(ILogger logger, string uploadId, string errorType, string message);

        [LoggerMessage(Level = LogLevel.Information, Message = "Quality check successful for upload {UploadId}: {FaceCount} faces, complexity {Complexity}")]
        public static partial void QualityCheckSuccessful(ILogger logger, string uploadId, int faceCount, string complexity);

        [LoggerMessage(Level = LogLevel.Error, Message = "Quality check HTTP request error for upload {UploadId}")]
        public static partial void QualityCheckHttpRequestError(ILogger logger, string uploadId, Exception ex);

        [LoggerMessage(Level = LogLevel.Error, Message = "Quality check unexpected error for upload {UploadId}")]
        public static partial void QualityCheckUnexpectedError(ILogger logger, string uploadId, Exception ex);

        [LoggerMessage(Level = LogLevel.Information, Message = "Received process analysis request for upload {UploadId}, process {ProcessCode}, timeout {Timeout}s")]
        public static partial void ReceivedProcessAnalysisRequest(ILogger logger, string uploadId, string processCode, int timeout);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Process analysis returned null for upload {UploadId}, process {ProcessCode}")]
        public static partial void ProcessAnalysisReturnedNull(ILogger logger, string uploadId, string processCode);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Process analysis timed out for upload {UploadId}, process {ProcessCode} after {Timeout}s")]
        public static partial void ProcessAnalysisTimedOut(ILogger logger, string uploadId, string processCode, int timeout);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Process analysis upload not found for upload {UploadId}, process {ProcessCode}")]
        public static partial void ProcessAnalysisUploadNotFound(ILogger logger, string uploadId, string processCode);

        [LoggerMessage(Level = LogLevel.Error, Message = "Process analysis failed for upload {UploadId}, process {ProcessCode}: {ErrorType} - {Message}")]
        public static partial void ProcessAnalysisFailed(ILogger logger, string uploadId, string processCode, string errorType, string message);

        [LoggerMessage(Level = LogLevel.Information, Message = "Process analysis successful for upload {UploadId}, process {ProcessCode}: {IssueCount} issues in {AnalysisTimeSeconds:F2}s")]
        public static partial void ProcessAnalysisSuccessful(ILogger logger, string uploadId, string processCode, int issueCount, double analysisTimeSeconds);

        [LoggerMessage(Level = LogLevel.Error, Message = "Process analysis HTTP request error for upload {UploadId}, process {ProcessCode}")]
        public static partial void ProcessAnalysisHttpRequestError(ILogger logger, string uploadId, string processCode, Exception ex);

        [LoggerMessage(Level = LogLevel.Error, Message = "Process analysis unexpected error for upload {UploadId}, process {ProcessCode}")]
        public static partial void ProcessAnalysisUnexpectedError(ILogger logger, string uploadId, string processCode, Exception ex);

        [LoggerMessage(Level = LogLevel.Information, Message = "Received cleanup request for upload {UploadId}")]
        public static partial void ReceivedCleanupRequest(ILogger logger, string uploadId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Cleanup upload not found for upload {UploadId}")]
        public static partial void CleanupUploadNotFound(ILogger logger, string uploadId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Cleanup successful for upload {UploadId}")]
        public static partial void CleanupSuccessful(ILogger logger, string uploadId);

        [LoggerMessage(Level = LogLevel.Error, Message = "Cleanup HTTP request error for upload {UploadId}")]
        public static partial void CleanupHttpRequestError(ILogger logger, string uploadId, Exception ex);

        [LoggerMessage(Level = LogLevel.Error, Message = "Cleanup unexpected error for upload {UploadId}")]
        public static partial void CleanupUnexpectedError(ILogger logger, string uploadId, Exception ex);
    }
}
