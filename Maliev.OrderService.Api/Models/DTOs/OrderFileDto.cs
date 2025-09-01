using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.Models.DTOs
{
    /// <summary>
    /// Represents an order file data transfer object.
    /// </summary>
    public class OrderFileDto
    {
        /// <summary>
        /// Gets or sets the ID of the order file.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the bucket name where the file is stored. This field is required.
        /// </summary>
        public string Bucket { get; set; } = null!;

        /// <summary>
        /// Gets or sets the object name (file name) of the order file. This field is required.
        /// </summary>
        public string ObjectName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the ID of the order associated with this file. This field is required.
        /// </summary>
        public int OrderId { get; set; }
    }
}