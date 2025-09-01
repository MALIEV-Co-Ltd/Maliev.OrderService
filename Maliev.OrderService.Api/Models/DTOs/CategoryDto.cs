using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.Models.DTOs
{
    /// <summary>
    /// Represents a category data transfer object.
    /// </summary>
    public class CategoryDto
    {
        /// <summary>
        /// Gets or sets the ID of the category.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the category.
        /// </summary>
        public string? Name { get; set; }
    }
}