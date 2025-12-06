using System.Net.Http.Json;

namespace Maliev.OrderService.Api.Services.External;

/// <summary>
/// Client for interacting with the external Customer Service
/// </summary>
public partial class CustomerServiceClient : ICustomerServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomerServiceClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerServiceClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client</param>
    /// <param name="logger">The logger instance</param>
    public CustomerServiceClient(HttpClient httpClient, ILogger<CustomerServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> HasActiveNdaAsync(string customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/customers/{customerId}/nda-status", cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<NdaStatusResponse>(cancellationToken: cancellationToken);
            return result?.HasActiveNda ?? false;
        }
        catch (HttpRequestException ex)
        {
            Log.FailedToCheckNdaStatus(_logger, customerId, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<CustomerDetailsDto?> GetCustomerDetailsAsync(string customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/customers/{customerId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<CustomerDetailsDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Log.FailedToGetCustomerDetails(_logger, customerId, ex);
            return null;
        }
    }

    private sealed class NdaStatusResponse
    {
        public bool HasActiveNda { get; set; }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to check NDA status for customer {CustomerId}")]
        public static partial void FailedToCheckNdaStatus(ILogger logger, string customerId, Exception ex);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get customer details for {CustomerId}")]
        public static partial void FailedToGetCustomerDetails(ILogger logger, string customerId, Exception ex);
    }
}
