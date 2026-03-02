namespace Maliev.OrderService.Application.DTOs.Response
{
    /// <summary>
    /// Response model for an order file attachment.
    /// </summary>
    public class OrderFileResponse
    {
        /// <summary>Gets or sets the file identifier.</summary>
        public long FileId { get; set; }

        /// <summary>Gets or sets the order identifier.</summary>
        public required string OrderId { get; set; }

        /// <summary>Gets or sets the original file name.</summary>
        public required string FileName { get; set; }

        /// <summary>Gets or sets the file role (Input, Output, Supporting).</summary>
        public required string FileRole { get; set; }

        /// <summary>Gets or sets the file category.</summary>
        public required string FileCategory { get; set; }

        /// <summary>Gets or sets the file size in bytes.</summary>
        public long FileSize { get; set; }

        /// <summary>Gets or sets the file MIME type.</summary>
        public required string FileType { get; set; }

        /// <summary>Gets or sets the GCS object storage path.</summary>
        public required string ObjectPath { get; set; }

        /// <summary>Gets or sets the access level (Internal or Confidential).</summary>
        public required string AccessLevel { get; set; }

        /// <summary>Gets or sets the design units (for CAD files).</summary>
        public string? DesignUnits { get; set; }

        /// <summary>Gets or sets who uploaded the file.</summary>
        public required string UploadedBy { get; set; }

        /// <summary>Gets or sets when the file was uploaded.</summary>
        public DateTime UploadedAt { get; set; }

        /// <summary>Gets or sets when the file was soft-deleted.</summary>
        public DateTime? DeletedAt { get; set; }
    }
}
