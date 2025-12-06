namespace Maliev.OrderService.Api.DTOs.Response;

/// <summary>
/// Response model for successful batch order cancellation.
/// </summary>
public class SuccessBatchCancellationResponse
{
    /// <summary>
    /// Gets or sets the success message.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Gets or sets the results of individual order cancellations.
    /// </summary>
    public required List<object> Results { get; set; }
}
