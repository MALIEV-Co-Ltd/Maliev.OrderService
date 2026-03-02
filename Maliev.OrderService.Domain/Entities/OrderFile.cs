namespace Maliev.OrderService.Domain.Entities
{
    /// <summary>
    /// Represents a file attachment associated with an order.
    /// </summary>
    public class OrderFile
    {
        /// <summary>Gets or sets the file identifier.</summary>
        public long FileId { get; set; }

        /// <summary>Gets or sets the order identifier.</summary>
        public string OrderId { get; set; } = null!;

        /// <summary>Gets or sets the file role (Input, Output, Supporting).</summary>
        public string FileRole { get; set; } = null!;

        /// <summary>Gets or sets the file category (CAD, Drawing, Image, Document, Archive, Other).</summary>
        public string FileCategory { get; set; } = null!;

        /// <summary>Gets or sets the design units (mm, inch, cm, m) — nullable, CAD files only.</summary>
        public string? DesignUnits { get; set; }

        /// <summary>Gets or sets the GCS object storage path.</summary>
        public string ObjectPath { get; set; } = null!;

        /// <summary>Gets or sets the original file name.</summary>
        public string FileName { get; set; } = null!;

        /// <summary>Gets or sets the file size in bytes (max 100MB).</summary>
        public long FileSize { get; set; }

        /// <summary>Gets or sets the MIME type or file extension.</summary>
        public string FileType { get; set; } = null!;

        /// <summary>Gets or sets the access level (Internal or Confidential).</summary>
        public string AccessLevel { get; set; } = "Internal";

        /// <summary>Gets or sets when the file was uploaded.</summary>
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Gets or sets who uploaded the file.</summary>
        public string UploadedBy { get; set; } = null!;

        /// <summary>Gets or sets when the file was soft-deleted (30-day retention).</summary>
        public DateTime? DeletedAt { get; set; }

        // Navigation Properties
        /// <summary>Gets or sets the parent order navigation property.</summary>
        public Order Order { get; set; } = null!;
    }
}
