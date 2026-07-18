using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Application.DTOs.Request
{
    /// <summary>
    /// Request model for creating a new order note.
    /// </summary>
    public class CreateOrderNoteRequest
    {
        /// <summary>Gets or sets the note type (customer or internal).</summary>
        [Required(ErrorMessage = "Note type is required")]
        [RegularExpression("^(customer|internal)$", ErrorMessage = "Note type must be 'customer' or 'internal'")]
        public required string NoteType { get; set; }

        /// <summary>Gets or sets the note text content.</summary>
        [Required(ErrorMessage = "Note text is required")]
        public required string NoteText { get; set; }
    }
}
