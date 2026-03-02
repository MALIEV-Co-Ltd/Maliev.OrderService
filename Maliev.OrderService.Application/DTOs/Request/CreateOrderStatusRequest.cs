using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Application.DTOs.Request
{
    /// <summary>
    /// Request model for creating a new order status entry.
    /// </summary>
    public class CreateOrderStatusRequest
    {
        /// <summary>Gets or sets the new status value.</summary>
        [Required(ErrorMessage = "Status is required")]
        public required string Status { get; set; }

        /// <summary>Gets or sets internal notes (employee-only).</summary>
        public string? InternalNotes { get; set; }

        /// <summary>Gets or sets customer-visible notes.</summary>
        public string? CustomerNotes { get; set; }
    }
}
