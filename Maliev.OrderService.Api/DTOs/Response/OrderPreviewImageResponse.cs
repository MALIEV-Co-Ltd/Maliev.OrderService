namespace Maliev.OrderService.Api.DTOs.Response;

/// <summary>
/// Response DTO for preview image metadata.
/// </summary>
public class OrderPreviewImageResponse
{
    /// <summary>Gets or sets the preview image ID.</summary>
    public long PreviewImageId { get; set; }

    /// <summary>Gets or sets the side name (Front, Left, Right, Back, Top, Bottom).</summary>
    public string Side { get; set; } = null!;

    /// <summary>Gets or sets the GCS storage path.</summary>
    public string StoragePath { get; set; } = null!;

    /// <summary>Gets or sets when the preview was generated.</summary>
    public DateTime GeneratedAt { get; set; }
}
