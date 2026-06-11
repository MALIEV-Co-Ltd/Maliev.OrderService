using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.DTOs.Request
{
    /// <summary>
    /// Request model for creating a status entry on an order
    /// </summary>
    public class CreateOrderStatusRequest
    {
        /// <summary>Gets or sets the status value</summary>
        [Required(ErrorMessage = "Status is required")]
        [RegularExpression("^(New|Reviewing|Rejected|Reviewed|Quoted|Declined|Accepted|Expired|Paid|POIssued|InProgress|OnHold|Finished|Shipped|Reopen|Cancelled)$",
            ErrorMessage = "Status must be one of: New, Reviewing, Rejected, Reviewed, Quoted, Declined, Accepted, Expired, Paid, POIssued, InProgress, OnHold, Finished, Shipped, Reopen, Cancelled")]
        public required string Status { get; set; }

        /// <summary>Gets or sets internal notes (only visible to employees)</summary>
        [MaxLength(2000, ErrorMessage = "Internal Notes must not exceed 2000 characters")]
        public string? InternalNotes { get; set; }

        /// <summary>Gets or sets customer-facing notes</summary>
        [MaxLength(2000, ErrorMessage = "Customer Notes must not exceed 2000 characters")]
        public string? CustomerNotes { get; set; }

        /// <summary>Gets or sets the payment transaction identifier for paid transitions.</summary>
        [MaxLength(100, ErrorMessage = "Payment ID must not exceed 100 characters")]
        public string? PaymentId { get; set; }
    }
}
