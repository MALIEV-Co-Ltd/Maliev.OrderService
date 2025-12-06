namespace Maliev.OrderService.Api.Configuration;

/// <summary>
/// Configuration options for external service clients.
/// </summary>
public class ExternalServiceOptions
{
    /// <summary>
    /// Gets or sets the base URL for the external service.
    /// </summary>
    public required string BaseUrl { get; set; }
    /// <summary>
    /// Gets or sets the timeout for external service calls in seconds.
    /// Default is 180 seconds.
    /// </summary>
    public int TimeoutInSeconds { get; set; } = 180;
}
