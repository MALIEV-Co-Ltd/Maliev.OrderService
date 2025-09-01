using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.Models.DTOs
{
    /// <summary>
    /// Represents a request to create a new file format.
    /// </summary>
    public class CreateFileFormatRequest
    {
        /// <summary>
        /// Gets or sets the name of the file format. This field is required.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the extension of the file format. This field is required.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Extension { get; set; } = null!;
    }
}