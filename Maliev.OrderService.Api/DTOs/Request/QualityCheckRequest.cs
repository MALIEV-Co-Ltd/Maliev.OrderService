namespace Maliev.OrderService.Api.DTOs.Request;

/// <summary>
/// Request for quality check on uploaded file (Phase 1 of two-phase DFM analysis)
/// </summary>
public class QualityCheckRequest
{
    /// <summary>
    /// Base64-encoded STL file bytes
    /// </summary>
    public string StlBytes { get; set; } = string.Empty;

    /// <summary>
    /// Optional base64-encoded CAD file bytes (STEP/IGES)
    /// </summary>
    public string? CadBytes { get; set; }

    /// <summary>
    /// Optional CAD file extension (e.g., "step", "stp", "igs", "iges")
    /// </summary>
    public string? CadExtension { get; set; }
}
