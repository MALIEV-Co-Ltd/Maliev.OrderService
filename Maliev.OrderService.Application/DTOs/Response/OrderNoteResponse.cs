namespace Maliev.OrderService.Application.DTOs.Response
{
    /// <summary>
    /// Response model for an order note.
    /// </summary>
    public class OrderNoteResponse
    {
        /// <summary>Gets or sets the note identifier.</summary>
        public long NoteId { get; set; }

        /// <summary>Gets or sets the order identifier.</summary>
        public required string OrderId { get; set; }

        /// <summary>Gets or sets the note type (customer or internal).</summary>
        public required string NoteType { get; set; }

        /// <summary>Gets or sets the note text content.</summary>
        public required string NoteText { get; set; }

        /// <summary>Gets or sets who created the note.</summary>
        public required string CreatedBy { get; set; }

        /// <summary>Gets or sets when the note was created.</summary>
        public DateTime CreatedAt { get; set; }
    }
}
