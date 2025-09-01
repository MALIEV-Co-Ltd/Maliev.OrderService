using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.Models.DTOs
{
    /// <summary>
    /// Represents a request to create a new order file.
    /// </summary>
    public class CreateOrderFileRequest
    {
        /// <summary>
        /// Gets or sets the bucket name where the file is stored. This field is required.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Bucket { get; set; } = null!;

        /// <summary>
        /// Gets or sets the object name (file name) of the order file. This field is required.
        /// </summary>
        [Required]
        public string ObjectName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the ID of the order associated with this file. This field is required.
        /// </summary>
        [Required]
        public int OrderId { get; set; }
    }
}