namespace Maliev.OrderService.Api.DTOs.Response;

/// <summary>
/// Response from process-specific DFM analysis (Phase 2 of two-phase DFM analysis)
/// </summary>
public class DfmAnalysisResponse
{
    /// <summary>
    /// Unique identifier for the upload
    /// </summary>
    public string UploadId { get; set; } = string.Empty;

    /// <summary>
    /// Manufacturing process code analyzed (e.g., "FDM", "SLA", "CNC_MILL")
    /// </summary>
    public string ProcessCode { get; set; } = string.Empty;

    /// <summary>
    /// Status of the analysis ("analysis_complete", "timeout", "error")
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// DFM analysis report
    /// </summary>
    public DfmReport DfmReport { get; set; } = new();

    /// <summary>
    /// Error type (if status is "error" or "timeout")
    /// </summary>
    public string? ErrorType { get; set; }

    /// <summary>
    /// Error message (if status is "error" or "timeout")
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// DFM analysis report for a specific manufacturing process
/// </summary>
public class DfmReport
{
    /// <summary>
    /// Type of report (matches process code)
    /// </summary>
    public string ReportType { get; set; } = string.Empty;

    /// <summary>
    /// List of DFM issues found
    /// </summary>
    public List<DfmIssue> Issues { get; set; } = new();

    /// <summary>
    /// Analysis time in seconds
    /// </summary>
    public double AnalysisTimeSeconds { get; set; }

    /// <summary>
    /// Number of thin wall issues (printing processes)
    /// </summary>
    public int? ThinWallCount { get; set; }

    /// <summary>
    /// Number of overhang faces (printing processes)
    /// </summary>
    public int? OverhangFaceCount { get; set; }

    /// <summary>
    /// Whether support is required (printing processes)
    /// </summary>
    public bool? SupportRequired { get; set; }

    /// <summary>
    /// Estimated support volume in cubic centimeters (printing processes)
    /// </summary>
    public double? EstimatedSupportVolumeCm3 { get; set; }

    /// <summary>
    /// Minimum internal radius found (CNC processes)
    /// </summary>
    public double? MinInternalRadius { get; set; }

    /// <summary>
    /// Number of deep cavities (CNC processes)
    /// </summary>
    public int? DeepCavityCount { get; set; }

    /// <summary>
    /// Whether sharp corners detected (CNC processes)
    /// </summary>
    public bool? SharpCornersDetected { get; set; }
}

/// <summary>
/// Individual DFM issue detected
/// </summary>
public class DfmIssue
{
    /// <summary>
    /// Category of issue (e.g., "thin_wall", "overhang", "hole")
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Severity level ("error", "warning", "info")
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Title of the issue
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the issue
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Actual value measured
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Threshold value that was exceeded
    /// </summary>
    public double Threshold { get; set; }

    /// <summary>
    /// Face indices where issue occurs (for visualization)
    /// </summary>
    public List<int> FaceIndices { get; set; } = new();

    /// <summary>
    /// Centroid coordinates [x, y, z] in millimeters (for visualization)
    /// </summary>
    public List<double> Centroid { get; set; } = new();

    /// <summary>
    /// Additional metadata about the issue
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
