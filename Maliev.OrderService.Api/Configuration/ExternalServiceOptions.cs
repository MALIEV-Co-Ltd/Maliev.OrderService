namespace Maliev.OrderService.Api.Configuration;

public class ExternalServiceOptions
{
    public required string BaseUrl { get; set; }
    public int TimeoutInSeconds { get; set; } = 180;
}
