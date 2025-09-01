using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.Models.DTOs
{
    /// <summary>
    /// Represents a request to create a new process.
    /// </summary>
    public class CreateProcessRequest
    {
        /// <summary>
        /// Gets or sets the name of the process. This field is required.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the category ID associated with the process. This field is required.
        /// </summary>
        [Required]
        public int CategoryId { get; set; }
    }
}