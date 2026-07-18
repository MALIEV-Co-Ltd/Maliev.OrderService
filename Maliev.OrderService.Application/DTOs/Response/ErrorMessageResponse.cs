namespace Maliev.OrderService.Application.DTOs.Response
{
    /// <summary>
    /// Standard error response with a message.
    /// </summary>
    public class ErrorMessageResponse
    {
        /// <summary>Gets or sets the error message.</summary>
        public required string Message { get; set; }

        /// <summary>Gets or sets additional error details.</summary>
        public string? Error { get; set; }
    }

    /// <summary>
    /// Standard success response with a message.
    /// </summary>
    public class SuccessMessageResponse
    {
        /// <summary>Gets or sets the success message.</summary>
        public required string Message { get; set; }
    }

    /// <summary>
    /// Success response with a message and reason.
    /// </summary>
    public class SuccessMessageWithReasonResponse
    {
        /// <summary>Gets or sets the success message.</summary>
        public required string Message { get; set; }

        /// <summary>Gets or sets the reason or additional details.</summary>
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Response for batch cancellation operations.
    /// </summary>
    public class SuccessBatchCancellationResponse
    {
        /// <summary>Gets or sets the success message.</summary>
        public required string Message { get; set; }

        /// <summary>Gets or sets the individual cancellation results.</summary>
        public required List<object> Results { get; set; }
    }

    /// <summary>
    /// Error response for batch validation failures.
    /// </summary>
    public class BatchValidationErrorResponse
    {
        /// <summary>Gets or sets the error message.</summary>
        public required string Message { get; set; }

        /// <summary>Gets or sets the validation errors.</summary>
        public required List<object> Errors { get; set; }

        /// <summary>Gets or sets the index of the failing item in the batch.</summary>
        public int ItemIndex { get; set; }
    }
}
