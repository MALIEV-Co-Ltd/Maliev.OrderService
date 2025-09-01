using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.Models.DTOs
{
    /// <summary>
    /// Represents a request to update an existing category.
    /// </summary>
    public class UpdateCategoryRequest
    {
        /// <summary>
        /// Gets or sets the ID of the category to update. This field is required.
        /// </summary>
        [Required]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the new name of the category. This field is required.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = null!;
    }
}