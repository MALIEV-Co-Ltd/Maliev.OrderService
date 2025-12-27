using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.DTOs.Request
{
    /// <summary>
    /// Request model for creating a note on an order
    /// </summary>
    public class CreateOrderNoteRequest
    {
        /// <summary>Gets or sets the note type (customer or internal)</summary>
        [Required(ErrorMessage = "Note Type is required")]
        [RegularExpression("^(customer|internal)$", ErrorMessage = "Note Type must be one of: customer, internal")]
        public required string NoteType { get; set; }

        /// <summary>Gets or sets the note text content</summary>
        [Required(ErrorMessage = "Note Text is required")]
        [MaxLength(2000, ErrorMessage = "Note Text must not exceed 2000 characters")]
        public required string NoteText { get; set; }
    }
}
