namespace Maliev.OrderService.Data.Models;

public class OrderFile
{
    public long FileId { get; set; }
    public string OrderId { get; set; } = null!;
    public string FileRole { get; set; } = null!; // Input, Output, Supporting
    public string FileCategory { get; set; } = null!; // CAD, Drawing, Image, Document, Archive, Other
    public string? DesignUnits { get; set; } // mm, inch, cm, m (nullable, CAD files only)
    public string ObjectPath { get; set; } = null!; // GCS object path
    public string FileName { get; set; } = null!;
    public long FileSize { get; set; } // Size in bytes (max 100MB)
    public string FileType { get; set; } = null!; // MIME type or extension
    public string AccessLevel { get; set; } = "Internal"; // Internal or Confidential
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string UploadedBy { get; set; } = null!;
    public DateTime? DeletedAt { get; set; } // Soft delete (30-day retention)

    // Navigation Properties
    public Order Order { get; set; } = null!;
}
