namespace Maliev.OrderService.Data.Models;

public class OrderNote
{
    public long NoteId { get; set; }
    public string OrderId { get; set; } = null!;
    public string NoteType { get; set; } = null!; // customer or internal
    public string NoteText { get; set; } = null!;
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Order Order { get; set; } = null!;
}
