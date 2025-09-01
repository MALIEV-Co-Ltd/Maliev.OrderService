using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.Models.DTOs
{
    /// <summary>
    /// Represents a request to create a new category.
    /// </summary>
    public class CreateCategoryRequest
    {
        /// <summary>
        /// Gets or sets the name of the category. This field is required.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = null!;
    }
}