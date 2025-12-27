namespace Maliev.OrderService.Api.DTOs.Response
{
    /// <summary>
    /// Success message response with a reason.
    /// </summary>
    public class SuccessMessageWithReasonResponse
    {
        /// <summary>
        /// Gets or sets the success message.
        /// </summary>
        public required string Message { get; set; }

        /// <summary>
        /// Gets or sets the reason for the success.
        /// </summary>
        public required string Reason { get; set; }
    }
}
