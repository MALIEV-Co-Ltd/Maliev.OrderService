using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.Models.DTOs
{
    /// <summary>
    /// Represents a request to update an existing file format.
    /// </summary>
    public class UpdateFileFormatRequest
    {
        /// <summary>
        /// Gets or sets the ID of the file format to update. This field is required.
        /// </summary>
        [Required]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the new name of the file format. This field is required.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the new extension of the file format. This field is required.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Extension { get; set; } = null!;
    }
}