namespace Maliev.OrderService.Api.DTOs.Response;

/// <summary>
/// Generic success message response.
/// </summary>
public class SuccessMessageResponse
{
    /// <summary>
    /// Gets or sets the success message.
    /// </summary>
    public required string Message { get; set; }
}
