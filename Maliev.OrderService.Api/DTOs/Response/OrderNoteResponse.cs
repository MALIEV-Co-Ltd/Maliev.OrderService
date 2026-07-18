namespace Maliev.OrderService.Api.DTOs.Response
{
    /// <summary>
    /// Response model for order notes and comments
    /// </summary>
    public class OrderNoteResponse
    {
        /// <summary>Gets or sets the note unique identifier</summary>
        public required long NoteId { get; set; }

        /// <summary>Gets or sets the order ID this note belongs to</summary>
        public required string OrderId { get; set; }

        /// <summary>Gets or sets the note type (e.g., Internal, Customer)</summary>
        public required string NoteType { get; set; }

        /// <summary>Gets or sets the note text content</summary>
        public required string NoteText { get; set; }

        /// <summary>Gets or sets who created this note</summary>
        public required string CreatedBy { get; set; }

        /// <summary>Gets or sets when this note was created</summary>
        public required DateTime CreatedAt { get; set; }
    }
}
