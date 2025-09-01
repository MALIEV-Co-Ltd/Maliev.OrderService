using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.Models.DTOs
{
    /// <summary>
    /// Represents a process data transfer object.
    /// </summary>
    public class ProcessDto
    {
        /// <summary>
        /// Gets or sets the ID of the process.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the process. This field is required.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the category ID associated with the process. This field is required.
        /// </summary>
        public int CategoryId { get; set; }
    }
}