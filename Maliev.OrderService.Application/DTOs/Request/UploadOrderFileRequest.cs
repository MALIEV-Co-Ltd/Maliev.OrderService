using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Application.DTOs.Request
{
    /// <summary>
    /// Request model for uploading a file to an order.
    /// </summary>
    public class UploadOrderFileRequest
    {
        /// <summary>Gets or sets the file role (Input, Output, Supporting).</summary>
        [Required(ErrorMessage = "File role is required")]
        public required string FileRole { get; set; }

        /// <summary>Gets or sets the file category (CAD, Drawing, Image, Document, Archive, Other).</summary>
        [Required(ErrorMessage = "File category is required")]
        public required string FileCategory { get; set; }

        /// <summary>Gets or sets the design units (mm, inch, cm, m) — for CAD files only.</summary>
        public string? DesignUnits { get; set; }
    }
}
