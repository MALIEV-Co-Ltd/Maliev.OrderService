namespace Maliev.OrderService.Api.DTOs.Request;

public class CreateOrderNoteRequest
{
    public required string NoteType { get; set; } // customer or internal
    public required string NoteText { get; set; }
}
