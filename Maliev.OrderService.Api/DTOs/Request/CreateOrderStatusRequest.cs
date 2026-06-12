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
        [RegularExpression("^(New|Reviewing|Rejected|Reviewed|Quoted|Declined|Accepted|Expired|Paid|POIssued|InProgress|OnHold|Finished|QualityReleased|Shipped|Reopen|Cancelled)$",
            ErrorMessage = "Status must be one of: New, Reviewing, Rejected, Reviewed, Quoted, Declined, Accepted, Expired, Paid, POIssued, InProgress, OnHold, Finished, QualityReleased, Shipped, Reopen, Cancelled")]
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

        /// <summary>Gets or sets the actual amount received for paid transitions.</summary>
        [Range(0, double.MaxValue, ErrorMessage = "Paid amount must be greater than or equal to 0")]
        public decimal? PaidAmount { get; set; }

        /// <summary>Gets or sets the currency code for the actual payment received.</summary>
        [MaxLength(3, ErrorMessage = "Payment currency must not exceed 3 characters")]
        public string? PaymentCurrency { get; set; }

        /// <summary>Gets or sets the payment provider for paid transitions.</summary>
        [MaxLength(100, ErrorMessage = "Payment provider name must not exceed 100 characters")]
        public string? PaymentProviderName { get; set; }
    }
}
