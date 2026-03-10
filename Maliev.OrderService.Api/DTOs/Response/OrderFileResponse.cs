namespace Maliev.OrderService.Api.DTOs.Response
{
    /// <summary>
    /// Response model for order file attachments
    /// </summary>
    public class OrderFileResponse
    {
        /// <summary>Gets or sets the file unique identifier</summary>
        public required long FileId { get; set; }

        /// <summary>Gets or sets the order ID this file belongs to</summary>
        public required string OrderId { get; set; }

        /// <summary>Gets or sets the file name</summary>
        public required string FileName { get; set; }

        /// <summary>Gets or sets the file role (e.g., Design, Quote, Reference)</summary>
        public required string FileRole { get; set; }

        /// <summary>Gets or sets the file category</summary>
        public required string FileCategory { get; set; }

        /// <summary>Gets or sets the file size in bytes</summary>
        public required long FileSize { get; set; }

        /// <summary>Gets or sets the file MIME type</summary>
        public required string FileType { get; set; }

        /// <summary>Gets or sets the object path in Upload Service</summary>
        public required string ObjectPath { get; set; }

        /// <summary>Gets or sets the access level (Public, Private, Employee)</summary>
        public required string AccessLevel { get; set; }

        /// <summary>Gets or sets the design units for design files</summary>
        public string? DesignUnits { get; set; }

        /// <summary>Gets or sets who uploaded this file</summary>
        public required string UploadedBy { get; set; }

        /// <summary>Gets or sets when this file was uploaded</summary>
        public required DateTime UploadedAt { get; set; }

        /// <summary>Gets or sets when this file was deleted (soft delete)</summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>Gets or sets whether this is the primary 3D file for the order</summary>
        public bool IsPrimary { get; set; }
    }
}
