namespace Maliev.OrderService.Api.DTOs.Response
{
    /// <summary>
    /// Generic error message response.
    /// </summary>
    public class ErrorMessageResponse
    {
        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public required string Message { get; set; }

        /// <summary>
        /// Gets or sets the detailed error information.
        /// </summary>
        public string? Error { get; set; }
    }
}
