using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.DTOs.Request
{
    /// <summary>
    /// Request model for uploading a file to an order
    /// </summary>
    public class UploadOrderFileRequest
    {
        /// <summary>Gets or sets the file role (Input, Output, Supporting)</summary>
        [Required(ErrorMessage = "File Role is required")]
        [RegularExpression("^(Input|Output|Supporting)$", ErrorMessage = "File Role must be one of: Input, Output, Supporting")]
        public required string FileRole { get; set; }

        /// <summary>Gets or sets the file category (CAD, Drawing, Image, Document, Archive, Other)</summary>
        [Required(ErrorMessage = "File Category is required")]
        [RegularExpression("^(CAD|Drawing|Image|Document|Archive|Other)$", ErrorMessage = "File Category must be one of: CAD, Drawing, Image, Document, Archive, Other")]
        public required string FileCategory { get; set; }

        /// <summary>Gets or sets the design units for CAD files (mm, inch, cm, m)</summary>
        [RegularExpression("^(mm|inch|cm|m)$", ErrorMessage = "Design Units must be one of: mm, inch, cm, m")]
        public string? DesignUnits { get; set; }
    }
}
