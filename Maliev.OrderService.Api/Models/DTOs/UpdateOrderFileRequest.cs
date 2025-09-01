using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.Models.DTOs
{
    /// <summary>
    /// Represents a request to update an existing order file.
    /// </summary>
    public class UpdateOrderFileRequest
    {
        /// <summary>
        /// Gets or sets the ID of the order file to update. This field is required.
        /// </summary>
        [Required]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the new bucket name where the file is stored. This field is required.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Bucket { get; set; } = null!;

        /// <summary>
        /// Gets or sets the new object name (file name) of the order file. This field is required.
        /// </summary>
        [Required]
        public string ObjectName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the new order ID associated with this file. This field is required.
        /// </summary>
        [Required]
        public int OrderId { get; set; }
    }
}