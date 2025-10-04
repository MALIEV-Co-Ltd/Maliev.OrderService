namespace Maliev.OrderService.Api.Services.External;

public interface ICustomerServiceClient
{
    Task<bool> HasActiveNdaAsync(string customerId, CancellationToken cancellationToken = default);
    Task<CustomerDetailsDto?> GetCustomerDetailsAsync(string customerId, CancellationToken cancellationToken = default);
}

public class CustomerDetailsDto
{
    public required string CustomerId { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public bool HasActiveNda { get; set; }
}
