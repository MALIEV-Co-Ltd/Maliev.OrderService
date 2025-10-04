namespace Maliev.OrderService.Api.DTOs.Response;

public class OrderNoteResponse
{
    public required long NoteId { get; set; }
    public required string OrderId { get; set; }
    public required string NoteType { get; set; }
    public required string NoteText { get; set; }
    public required string CreatedBy { get; set; }
    public required DateTime CreatedAt { get; set; }
}
