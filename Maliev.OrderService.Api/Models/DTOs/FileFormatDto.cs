using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.Models.DTOs
{
    /// <summary>
    /// Represents a file format data transfer object.
    /// </summary>
    public class FileFormatDto
    {
        /// <summary>
        /// Gets or sets the ID of the file format.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the file format.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the extension of the file format.
        /// </summary>
        public string? Extension { get; set; }
    }
}