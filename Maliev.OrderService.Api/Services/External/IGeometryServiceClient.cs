using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;

namespace Maliev.OrderService.Api.Services.External;

/// <summary>
/// Client interface for interacting with the Geometry Service
/// </summary>
public interface IGeometryServiceClient
{
    /// <summary>
    /// Perform quality check on uploaded file (Phase 1 of two-phase DFM analysis)
    /// </summary>
    /// <param name="uploadId">Unique identifier for the upload</param>
    /// <param name="request">Quality check request with file data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quality check response</returns>
    Task<QualityCheckResponse?> QualityCheckAsync(string uploadId, QualityCheckRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze file for a specific manufacturing process (Phase 2 of two-phase DFM analysis)
    /// </summary>
    /// <param name="uploadId">Unique identifier for the upload</param>
    /// <param name="processCode">Manufacturing process code (e.g., "FDM", "SLA", "CNC_MILL")</param>
    /// <param name="timeout">Timeout in seconds (default: 30)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>DFM analysis response</returns>
    Task<DfmAnalysisResponse?> AnalyzeForProcessAsync(string uploadId, string processCode, int timeout = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up cached file data for an upload
    /// </summary>
    /// <param name="uploadId">Unique identifier for the upload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cleanup successful, false otherwise</returns>
    Task<bool> CleanupUploadAsync(string uploadId, CancellationToken cancellationToken = default);
}
