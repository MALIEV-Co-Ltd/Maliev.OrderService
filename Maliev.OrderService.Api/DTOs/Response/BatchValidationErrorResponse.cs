namespace Maliev.OrderService.Api.DTOs.Response
{
    /// <summary>
    /// Response model for batch validation errors.
    /// </summary>
    public class BatchValidationErrorResponse
    {
        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public required string Message { get; set; }

        /// <summary>
        /// Gets or sets the list of validation errors.
        /// </summary>
        public required object Errors { get; set; }

        /// <summary>
        /// Gets or sets the index of the item that failed validation in the batch.
        /// </summary>
        public required int ItemIndex { get; set; }
    }
}
