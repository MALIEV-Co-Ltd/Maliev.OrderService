namespace Maliev.OrderService.Api.DTOs.Response;

/// <summary>
/// Response model for order status history entries
/// </summary>
public class OrderStatusResponse
{
    /// <summary>Gets or sets the status record unique identifier</summary>
    public required long StatusId { get; set; }
    
    /// <summary>Gets or sets the order ID this status belongs to</summary>
    public required string OrderId { get; set; }
    
    /// <summary>Gets or sets the status value</summary>
    public required string Status { get; set; }
    
    /// <summary>Gets or sets internal notes (only visible to employees)</summary>
    public string? InternalNotes { get; set; }
    
    /// <summary>Gets or sets customer-facing notes</summary>
    public string? CustomerNotes { get; set; }
    
    /// <summary>Gets or sets who created this status update</summary>
    public required string UpdatedBy { get; set; }
    
    /// <summary>Gets or sets when this status was created</summary>
    public required DateTime Timestamp { get; set; }
}
