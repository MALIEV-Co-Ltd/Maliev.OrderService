using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.Models.DTOs
{
    /// <summary>
    /// Represents a request to update an existing process.
    /// </summary>
    public class UpdateProcessRequest
    {
        /// <summary>
        /// Gets or sets the ID of the process to update. This field is required.
        /// </summary>
        [Required]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the new name of the process. This field is required.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the new category ID associated with the process. This field is required.
        /// </summary>
        [Required]
        public int CategoryId { get; set; }
    }
}