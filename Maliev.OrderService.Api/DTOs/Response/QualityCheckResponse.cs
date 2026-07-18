namespace Maliev.OrderService.Api.DTOs.Response;

/// <summary>
/// Response from quality check (Phase 1 of two-phase DFM analysis)
/// </summary>
public class QualityCheckResponse
{
    /// <summary>
    /// Unique identifier for the upload
    /// </summary>
    public string UploadId { get; set; } = string.Empty;

    /// <summary>
    /// Status of the quality check ("quality_check_complete", "error")
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Quality metrics from the check
    /// </summary>
    public QualityMetrics Quality { get; set; } = new();

    /// <summary>
    /// Whether the file is ready for process selection
    /// </summary>
    public bool ReadyForProcessSelection { get; set; }

    /// <summary>
    /// Error type (if status is "error")
    /// </summary>
    public string? ErrorType { get; set; }

    /// <summary>
    /// Error message (if status is "error")
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// Quality metrics from quality check
/// </summary>
public class QualityMetrics
{
    /// <summary>
    /// Whether the mesh is manifold (watertight)
    /// </summary>
    public bool IsManifold { get; set; }

    /// <summary>
    /// Whether the mesh is empty (no faces)
    /// </summary>
    public bool IsEmpty { get; set; }

    /// <summary>
    /// Number of faces in the mesh
    /// </summary>
    public int FaceCount { get; set; }

    /// <summary>
    /// Number of vertices in the mesh
    /// </summary>
    public int VertexCount { get; set; }

    /// <summary>
    /// Volume in cubic millimeters
    /// </summary>
    public double VolumeMm3 { get; set; }

    /// <summary>
    /// Surface area in square millimeters
    /// </summary>
    public double SurfaceAreaMm2 { get; set; }

    /// <summary>
    /// Bounding box dimensions
    /// </summary>
    public BoundingBox BoundingBox { get; set; } = new();

    /// <summary>
    /// Whether the file can be previewed
    /// </summary>
    public bool CanPreview { get; set; }

    /// <summary>
    /// Complexity classification ("simple", "medium", "complex")
    /// </summary>
    public string Complexity { get; set; } = string.Empty;

    /// <summary>
    /// Number of bodies detected
    /// </summary>
    public int BodyCount { get; set; }

    /// <summary>
    /// Optional B-Rep face count (if CAD file provided)
    /// </summary>
    public int? BrepFaceCount { get; set; }
}

/// <summary>
/// Bounding box dimensions
/// </summary>
public class BoundingBox
{
    /// <summary>
    /// X dimension (width) in millimeters
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y dimension (depth) in millimeters
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Z dimension (height) in millimeters
    /// </summary>
    public double Z { get; set; }
}
